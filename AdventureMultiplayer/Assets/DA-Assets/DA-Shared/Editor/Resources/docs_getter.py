import json
import hashlib
import re
import sys
import time
import ssl
import html
from pathlib import Path
from urllib.request import urlopen, Request
from urllib.error import URLError, HTTPError
from urllib.parse import urljoin, urlparse, urlunparse
from typing import Optional, Dict, List
from dataclasses import dataclass, asdict
from html.parser import HTMLParser


@dataclass
class Section:
    name: str
    url: str
    subsections: list = None
    
    def __post_init__(self):
        if self.subsections is None:
            self.subsections = []
    
    def to_dict(self):
        result = {"name": self.name, "url": self.url}
        if self.subsections:
            result["subsections"] = [s.to_dict() if isinstance(s, Section) else s for s in self.subsections]
        return result


@dataclass 
class PageContent:
    title: str
    url: str
    content: str
    anchors: list
    navigation: dict
    elements: list
    images: list
    source_url: str
    
    def to_dict(self):
        return asdict(self)


class SimpleHTMLParser(HTMLParser):
    def __init__(self):
        super().__init__()
        self.result = []
        self.tag_stack = []
        
    def handle_starttag(self, tag, attrs):
        self.tag_stack.append((tag, dict(attrs)))
        
    def handle_endtag(self, tag):
        if self.tag_stack and self.tag_stack[-1][0] == tag:
            self.tag_stack.pop()
        
    def handle_data(self, data):
        if self.tag_stack:
            tag, attrs = self.tag_stack[-1]
            self.result.append({'tag': tag, 'attrs': attrs, 'text': data.strip(), 'depth': len(self.tag_stack)})


def extract_links_from_html(html_content: str) -> List[Dict[str, str]]:
    links = []
    pattern = r'<a[^>]*href=["\']([^"\']+)["\'][^>]*>(.*?)</a>'
    for match in re.finditer(pattern, html_content, re.IGNORECASE | re.DOTALL):
        href = match.group(1)
        text = re.sub(r'<[^>]+>', '', match.group(2)).strip()
        if text:
            links.append({'href': href, 'text': text})
    return links


def extract_text_content(html_content: str) -> str:
    content = re.sub(r'<(script|style|nav|aside)[^>]*>[\s\S]*?</\1>', '', html_content, flags=re.IGNORECASE)
    content = re.sub(r'<!--.*?-->', '', content, flags=re.DOTALL)
    
    def heading_replacer(m):
        level = int(m.group(1))
        text = re.sub(r'<[^>]+>', '', m.group(2)).strip()
        return f"\n\n{'#' * level} {text}\n"
    
    content = re.sub(r'<h([1-6])[^>]*>(.*?)</h\1>', heading_replacer, content, flags=re.IGNORECASE | re.DOTALL)
    content = re.sub(r'<li[^>]*>(.*?)</li>', r'\n- \1', content, flags=re.IGNORECASE | re.DOTALL)
    content = re.sub(r'<pre[^>]*>([\s\S]*?)</pre>', r'\n```\n\1\n```\n', content, flags=re.IGNORECASE)
    content = re.sub(r'<code[^>]*>(.*?)</code>', r'`\1`', content, flags=re.IGNORECASE | re.DOTALL)
    content = re.sub(r'<br\s*/?>', '\n', content, flags=re.IGNORECASE)
    content = re.sub(r'</(p|div)>', '\n\n', content, flags=re.IGNORECASE)
    content = re.sub(r'<[^>]+>', '', content)
    content = html.unescape(content)
    content = re.sub(r'\n{3,}', '\n\n', content)
    return content.strip()


def extract_title(html_content: str) -> str:
    match = re.search(r'<h1[^>]*>(.*?)</h1>', html_content, re.IGNORECASE | re.DOTALL)
    if match:
        title = re.sub(r'<[^>]+>', '', match.group(1))
        return html.unescape(title).strip()
    return ""


def extract_main_content(html_content: str) -> str:
    match = re.search(r'<main[^>]*>([\s\S]*?)</main>', html_content, re.IGNORECASE)
    if match:
        return match.group(1)
    return html_content


def extract_headings(html_content: str, url: str) -> List[Dict[str, str]]:
    anchors = []
    pattern = r'<h([2-4])[^>]*id=["\']([^"\']+)["\'][^>]*>(.*?)</h\1>'
    for match in re.finditer(pattern, html_content, re.IGNORECASE | re.DOTALL):
        heading_id = match.group(2)
        heading_text = re.sub(r'<[^>]+>', '', match.group(3)).strip()
        heading_text = html.unescape(heading_text)
        if heading_id and heading_text:
            anchors.append({'name': heading_text, 'anchor': f"#{heading_id}", 'url': f"{url}#{heading_id}"})
    return anchors


def build_markdown_url(url: str) -> str:
    parsed = urlparse(url)
    path = parsed.path.rstrip("/")
    if not path.endswith(".md"):
        path = f"{path}.md"
    return urlunparse((parsed.scheme, parsed.netloc, path, "", parsed.query, parsed.fragment))


def split_agent_instructions(markdown: str) -> str:
    marker = "\n---\n\n# Agent Instructions:"
    index = markdown.find(marker)
    if index == -1:
        return markdown
    return markdown[:index].strip()


def slugify_heading(text: str) -> str:
    value = re.sub(r"<[^>]+>", "", text)
    value = re.sub(r"[^\w\s-]", "", value, flags=re.UNICODE).strip().lower()
    value = re.sub(r"[\s_]+", "-", value)
    return value


def clean_markdown_text(text: str) -> str:
    text = text.replace("\\\n", "\n")
    text = text.replace("\\_", "_")
    text = re.sub(r"<(https?://[^>]+)>", r"\1", text)
    text = re.sub(r"<br\s*/?>", "\n", text, flags=re.IGNORECASE)
    text = re.sub(r"<[^>]+>", "", text)
    text = re.sub(r"\[([^\]]+)\]\([^)]+\)", r"\1", text)
    text = re.sub(r"(?m)^\s*[-*]\s+", "", text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"\1", text)
    text = re.sub(r"(?<!\*)\*([^*\n]+)\*(?!\*)", r"\1", text)
    text = re.sub(r"`([^`]+)`", r"\1", text)
    return html.unescape(text).strip().rstrip("\\").strip()


def extract_html_attrs(tag: str) -> Dict[str, str]:
    attrs = {}
    pattern = r'([a-zA-Z_:][-a-zA-Z0-9_:.]*)=["\']([^"\']*)["\']'
    for match in re.finditer(pattern, tag):
        attrs[match.group(1)] = html.unescape(match.group(2))
    return attrs


def parse_gitbook_markdown(markdown: str, url: str, root_url: str) -> dict:
    body = split_agent_instructions(markdown)
    lines = body.splitlines()
    elements = []
    images = []
    anchors = []
    title = ""
    step_index = 0
    in_code = False
    code_lang = ""
    code_lines = []
    in_hint = False
    hint_style = ""
    hint_lines = []

    def current_context() -> dict:
        context = {}
        if step_index > 0:
            context["step"] = step_index
        return context

    def add_text(kind: str, text: str):
        clean = clean_markdown_text(text)
        if clean:
            item = {"type": kind, "text": clean}
            item.update(current_context())
            elements.append(item)

    for raw_line in lines:
        line = raw_line.rstrip()
        stripped = line.strip()

        if in_code:
            if stripped.startswith("```"):
                item = {"type": "code", "language": code_lang, "text": "\n".join(code_lines)}
                item.update(current_context())
                elements.append(item)
                in_code = False
                code_lang = ""
                code_lines = []
            else:
                code_lines.append(raw_line)
            continue

        if in_hint:
            if stripped == "{% endhint %}":
                item = {"type": "hint", "style": hint_style, "text": clean_markdown_text("\n".join(hint_lines))}
                item.update(current_context())
                elements.append(item)
                in_hint = False
                hint_style = ""
                hint_lines = []
            else:
                hint_lines.append(raw_line)
            continue

        if stripped.startswith("```"):
            in_code = True
            code_lang = stripped[3:].strip()
            code_lines = []
            continue

        hint_match = re.match(r'\{%\s*hint\s+style=["\']?([^"\'}\s]+)["\']?\s*%\}', stripped)
        if hint_match:
            in_hint = True
            hint_style = hint_match.group(1)
            hint_lines = []
            continue

        if stripped == "{% stepper %}":
            elements.append({"type": "stepper_start"})
            continue
        if stripped == "{% endstepper %}":
            elements.append({"type": "stepper_end"})
            continue
        if stripped == "{% step %}":
            step_index += 1
            elements.append({"type": "step_start", "step": step_index})
            continue
        if stripped == "{% endstep %}":
            elements.append({"type": "step_end", "step": step_index})
            continue

        heading_match = re.match(r"^(#{1,6})\s+(.+)$", stripped)
        if heading_match:
            level = len(heading_match.group(1))
            text = clean_markdown_text(heading_match.group(2))
            if level == 1 and not title:
                title = text
            anchor = slugify_heading(text)
            item = {"type": "heading", "level": level, "text": text, "anchor": f"#{anchor}", "url": f"{url}#{anchor}"}
            item.update(current_context())
            elements.append(item)
            if level in (2, 3, 4):
                anchors.append({"name": text, "anchor": f"#{anchor}", "url": f"{url}#{anchor}"})
            continue

        img_match = re.search(r"<img\b[^>]*>", line, flags=re.IGNORECASE)
        if img_match:
            attrs = extract_html_attrs(img_match.group(0))
            src = attrs.get("src", "")
            image = {
                "type": "image",
                "src": src,
                "url": urljoin(root_url, src),
                "alt": attrs.get("alt", ""),
                "width": attrs.get("width", ""),
                "caption": clean_markdown_text(re.sub(r".*<figcaption>(.*?)</figcaption>.*", r"\1", line, flags=re.IGNORECASE | re.DOTALL)) if "<figcaption" in line else ""
            }
            image.update(current_context())
            images.append(image)
            elements.append(image)
            continue

        if re.match(r"^\s*[-*]\s+", line):
            add_text("list_item", re.sub(r"^\s*[-*]\s+", "", line))
            continue
        if re.match(r"^\s*\d+\.\s+", line):
            add_text("list_item", re.sub(r"^\s*\d+\.\s+", "", line))
            continue
        if stripped.startswith(">"):
            add_text("quote", stripped.lstrip("> "))
            continue
        if stripped and not stripped.startswith("<div") and not stripped.startswith("</div"):
            add_text("paragraph", stripped.rstrip("\\"))

    return {
        "title": title,
        "content": body.strip(),
        "anchors": anchors,
        "elements": elements,
        "images": images
    }


def fetch_url(url: str, timeout: int, user_agent: str) -> Optional[str]:
    try:
        ctx = ssl.create_default_context()
        ctx.check_hostname = False
        ctx.verify_mode = ssl.CERT_NONE
        request = Request(url, headers={'User-Agent': user_agent})
        with urlopen(request, timeout=timeout, context=ctx) as response:
            charset = response.headers.get_content_charset() or 'utf-8'
            return response.read().decode(charset)
    except HTTPError as e:
        print(f"HTTP Error {e.code} fetching {url}", file=sys.stderr)
        return None
    except URLError as e:
        print(f"URL Error fetching {url}: {e.reason}", file=sys.stderr)
        return None
    except Exception as e:
        print(f"Error fetching {url}: {e}", file=sys.stderr)
        return None


class CacheManager:
    def __init__(self, cache_dir: Path, expiry_hours: int, cache_enabled: bool):
        self.cache_dir = cache_dir
        self.expiry_seconds = expiry_hours * 3600
        self.cache_enabled = cache_enabled
        self.cache_dir.mkdir(parents=True, exist_ok=True)
    
    def _get_cache_path(self, url: str) -> Path:
        url_hash = hashlib.md5(url.encode()).hexdigest()
        return self.cache_dir / f"{url_hash}.json"
    
    def get(self, url: str) -> Optional[dict]:
        if not self.cache_enabled:
            return None
        cache_path = self._get_cache_path(url)
        if not cache_path.exists():
            return None
        try:
            with open(cache_path, 'r', encoding='utf-8') as f:
                cached = json.load(f)
            if time.time() - cached.get('timestamp', 0) > self.expiry_seconds:
                cache_path.unlink()
                return None
            return cached.get('data')
        except (json.JSONDecodeError, IOError):
            return None
    
    def set(self, url: str, data: dict):
        if not self.cache_enabled:
            return
        cache_path = self._get_cache_path(url)
        cached = {'timestamp': time.time(), 'url': url, 'data': data}
        with open(cache_path, 'w', encoding='utf-8') as f:
            json.dump(cached, f, ensure_ascii=False, indent=2)
    
    def clear(self):
        for cache_file in self.cache_dir.glob("*.json"):
            cache_file.unlink()


def extract_next_data(html_content: str) -> str:
    pattern = r'self\.__next_f\.push\(\[1,"(.*?)"\]\)'
    combined = []
    for match in re.finditer(pattern, html_content, re.DOTALL):
        try:
            escaped = match.group(1)
            unescaped = escaped.encode('utf-8').decode('unicode_escape')
            combined.append(unescaped)
        except (UnicodeDecodeError, ValueError):
            continue
    return "".join(combined)


def extract_json_block(source: str, start_index: int) -> Optional[str]:
    if start_index < 0 or start_index >= len(source):
        return None
    open_char = source[start_index]
    if open_char not in ('[', '{'):
        return None
    close_char = ']' if open_char == '[' else '}'
    balance = 0
    in_string = False
    escape_next = False
    for i in range(start_index, len(source)):
        ch = source[i]
        if escape_next:
            escape_next = False
            continue
        if ch == '\\':
            escape_next = True
            continue
        if ch == '"' and not escape_next:
            in_string = not in_string
            continue
        if not in_string:
            if ch == open_char:
                balance += 1
            elif ch == close_char:
                balance -= 1
                if balance == 0:
                    return source[start_index:i + 1]
    return None


def parse_pages_tree(pages: list, base_url: str) -> List[Section]:
    sections = []

    def build_url(href: Optional[str]) -> Optional[str]:
        if not href:
            return None
        if href.startswith("/"):
            url = urljoin(base_url, href)
        elif href.startswith("http"):
            url = href
        else:
            url = urljoin(base_url + "/", href)
        return url.split("#", 1)[0]

    def merge_sections(existing: List[Section], additions: List[Section]) -> List[Section]:
        if not additions:
            return existing
        seen = set()
        for item in existing:
            seen.add((item.name, item.url))
        for item in additions:
            key = (item.name, item.url)
            if key in seen:
                continue
            existing.append(item)
            seen.add(key)
        return existing

    i = 0
    total = len(pages)
    while i < total:
        page = pages[i]
        title = page.get("title")
        if not title:
            i += 1
            continue

        section = Section(name=title, url=build_url(page.get("href")))

        descendants = page.get("descendants") or []
        if descendants:
            section.subsections = parse_pages_tree(descendants, base_url)

        if page.get("type") == "group":
            grouped_pages = []
            j = i + 1
            while j < total and pages[j].get("type") != "group":
                grouped_pages.append(pages[j])
                j += 1
            if grouped_pages:
                grouped_sections = parse_pages_tree(grouped_pages, base_url)
                section.subsections = merge_sections(section.subsections, grouped_sections)
            sections.append(section)
            i = j
            continue

        sections.append(section)
        i += 1

    return sections


def extract_navigation_tree(html_content: str, base_url: str) -> List[Section]:
    next_data = extract_next_data(html_content)
    if not next_data:
        return []
    pages_index = next_data.find('"pages"')
    if pages_index == -1:
        return []
    object_start = next_data.rfind("{", 0, pages_index)
    json_block = extract_json_block(next_data, object_start)
    if not json_block:
        return []
    try:
        data = json.loads(json_block)
    except json.JSONDecodeError:
        return []
    pages = data.get("pages")
    if not isinstance(pages, list):
        return []
    return parse_pages_tree(pages, base_url)


class DocsGetter:
    def __init__(self, base_url: str, cache_dir: Path, cache_expiry_hours: int, 
                 user_agent: str, timeout: int, cache_enabled: bool):
        self.base_url = base_url.rstrip('/')
        parsed = urlparse(base_url)
        self.root_url = f"{parsed.scheme}://{parsed.netloc}"
        self.user_agent = user_agent
        self.timeout = timeout
        self.cache = CacheManager(cache_dir, cache_expiry_hours, cache_enabled)
    
    def _fetch(self, url: str) -> Optional[str]:
        return fetch_url(url, self.timeout, self.user_agent)
    
    def _build_absolute_url(self, href: str) -> Optional[str]:
        if not href:
            return None
        clean = href.split('#')[0]
        if clean.startswith("http"):
            return clean
        if clean.startswith("/"):
            return self.root_url + clean
        return self.root_url + "/" + clean
    
    def _extract_nav_links(self, html_content: str) -> List[Section]:
        sections = []
        seen_urls = set()
        links = extract_links_from_html(html_content)
        for link in links:
            href = link['href']
            text = link['text']
            if not text or not href:
                continue
            if href.startswith('/'):
                full_url = urljoin(self.root_url, href)
            elif href.startswith('http'):
                full_url = href
            else:
                continue
            parsed_base = urlparse(self.base_url)
            if parsed_base.netloc not in full_url:
                continue
            if '#' in href and not href.split('#')[0]:
                continue
            clean_url = full_url.split('#')[0]
            if clean_url not in seen_urls:
                seen_urls.add(clean_url)
                sections.append(Section(name=text, url=clean_url))
        return sections
    
    def _extract_page_content(self, html_content: str, url: str) -> PageContent:
        title = extract_title(html_content)
        main_content = extract_main_content(html_content)
        content_text = extract_text_content(main_content)
        anchors = extract_headings(html_content, url)
        navigation = {'prev': None, 'next': None}
        links = extract_links_from_html(html_content)
        for link in links:
            link_text = link['text'].lower()
            href = link['href']
            if 'previous' in link_text or 'prev' in link_text:
                if href.startswith('/'):
                    navigation['prev'] = {'name': link['text'].replace('Previous', '').strip(), 'url': urljoin(self.root_url, href)}
            elif 'next' in link_text:
                if href.startswith('/'):
                    navigation['next'] = {'name': link['text'].replace('Next', '').strip(), 'url': urljoin(self.root_url, href)}
        return PageContent(
            title=title,
            url=url,
            content=content_text,
            anchors=anchors,
            navigation=navigation,
            elements=[],
            images=[],
            source_url=url
        )
    
    def get_structure(self) -> dict:
        cache_key = f"{self.base_url}/__structure_v3__"
        cached = self.cache.get(cache_key)
        if cached:
            return cached
        html_content = self._fetch(self.base_url)
        if not html_content:
            return {"error": "Failed to fetch documentation"}
        sections = extract_navigation_tree(html_content, self.base_url)
        used_nav_tree = bool(sections)
        if not used_nav_tree:
            sections = self._extract_nav_links(html_content)
        result = {"base_url": self.base_url, "sections": [s.to_dict() for s in sections]}
        if used_nav_tree:
            self.cache.set(cache_key, result)
        return result
    
    def get_content(self, url: str) -> dict:
        markdown_url = build_markdown_url(url)
        cache_key = f"{markdown_url}/__content_v4__"
        cached = self.cache.get(cache_key)
        if cached:
            return cached
        markdown = self._fetch(markdown_url)
        if not markdown:
            return {"error": f"Failed to fetch {markdown_url}"}
        parsed = parse_gitbook_markdown(markdown, url, self.root_url)
        result = PageContent(
            title=parsed["title"],
            url=url,
            content=parsed["content"],
            anchors=parsed["anchors"],
            navigation={'prev': None, 'next': None},
            elements=parsed["elements"],
            images=parsed["images"],
            source_url=markdown_url
        ).to_dict()
        self.cache.set(cache_key, result)
        return result
    
    def get_documentation(self, url: Optional[str] = None) -> dict:
        if url is None:
            return self.get_structure()
        else:
            return self.get_content(url)


def main():
    import argparse
    
    parser = argparse.ArgumentParser()
    parser.add_argument('url', nargs='?', default=None)
    parser.add_argument('--base-url', type=str, required=True)
    parser.add_argument('--cache-dir', type=str, default=None)
    parser.add_argument('--cache-expiry', type=int, default=720)
    parser.add_argument('--user-agent', type=str, default='DocsGetter/1.0')
    parser.add_argument('--timeout', type=int, default=30)
    parser.add_argument('--cache-enabled', type=str, default='true')
    parser.add_argument('--clear-cache', action='store_true')
    
    args = parser.parse_args()
    
    cache_dir = Path(args.cache_dir) if args.cache_dir else Path(__file__).parent / ".docs_cache"
    cache_enabled = args.cache_enabled.lower() in ('true', '1', 'yes')
    
    getter = DocsGetter(
        base_url=args.base_url,
        cache_dir=cache_dir,
        cache_expiry_hours=args.cache_expiry,
        user_agent=args.user_agent,
        timeout=args.timeout,
        cache_enabled=cache_enabled
    )
    
    if args.clear_cache:
        getter.cache.clear()
        print("Cache cleared.")
        return
    
    result = getter.get_documentation(args.url)
    
    output = json.dumps(result, ensure_ascii=False, indent=2)
    if hasattr(sys.stdout, "buffer"):
        sys.stdout.buffer.write(output.encode("utf-8"))
        sys.stdout.buffer.write(b"\n")
    else:
        print(output)


if __name__ == "__main__":
    main()
