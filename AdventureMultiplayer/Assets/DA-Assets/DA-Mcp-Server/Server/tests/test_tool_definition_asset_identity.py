from da_mcp_server.transport.models import ToolDefinitionModel


def test_tool_definition_exposes_server_identity():
    tool = ToolDefinitionModel(
        name="get_frame_list",
        endpointPath="fcu",
        serverName="Figma Converter for Unity",
    )

    dumped = tool.model_dump()

    assert dumped == {
        "name": "get_frame_list",
        "description": "",
        "inputSchema": {},
        "annotations": {},
        "endpointPath": "fcu",
        "serverName": "Figma Converter for Unity",
        "requiresPolling": False,
        "pollAction": "status",
        "maxPollSeconds": 0,
    }
