using DA_Assets.Shared.MCP;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.UCC.MCP
{
    public class StepperTool : FcuMcpToolBase
    {
        public StepperTool(ConverterBase monoBeh, McpToolSO toolSO) : base(monoBeh, toolSO)
        {
        }

        protected override async Task<IReadOnlyList<ContentItem>> ExecuteWithContextAsync(ConverterBase monoBeh, Dictionary<string, object> args)
        {
            int stepIndex = 0;

            if (args.TryGetValue("step_index", out object stepIndexObj))
            {
                stepIndex = Convert.ToInt32(stepIndexObj);
            }

            SpriteDuplicateResolution spriteDuplicateResolution = ParseSpriteDuplicateResolution(args);

            ImportResult result = await monoBeh.ProjectImporter.Start_Import_Until_User_Action(stepIndex, spriteDuplicateResolution);
            bool requiresSpriteDuplicateChoice = result.PauseReason == ImportPauseReason.SpriteDuplicateRemover;

            string json = JsonUtility.ToJson(new StepperToolResult
            {
                status = result.Status.ToString(),
                message = result.Message,
                next_step = result.NextStep,
                requires_user_action = result.RequiresUserAction,
                pause_reason = result.PauseReason.ToString(),
                requires_explicit_user_choice = requiresSpriteDuplicateChoice,
                user_message = requiresSpriteDuplicateChoice
                    ? GetTemplate("sprite_duplicate_user_message")
                    : string.Empty,
                agent_instruction = requiresSpriteDuplicateChoice
                    ? GetTemplate("sprite_duplicate_agent_instruction")
                    : string.Empty,
                available_actions = Array.Empty<string>()
            }, true);

            return new[]
            {
                new ContentItem
                {
                    Type = "text",
                    Text = json
                }
            };
        }

        private static SpriteDuplicateResolution ParseSpriteDuplicateResolution(Dictionary<string, object> args)
        {
            if (!args.TryGetValue("sprite_duplicate_resolution", out object value) || value == null)
            {
                return SpriteDuplicateResolution.WaitForUser;
            }

            string normalized = value.ToString().Trim().Replace("_", "").Replace("-", "").ToLowerInvariant();
            return normalized switch
            {
                "applydefault" => SpriteDuplicateResolution.ApplyDefault,
                "keepallduplicates" => SpriteDuplicateResolution.KeepAllDuplicates,
                "waitforuser" => SpriteDuplicateResolution.WaitForUser,
                _ => SpriteDuplicateResolution.WaitForUser
            };
        }

        [Serializable]
        private struct StepperToolResult
        {
            public string status;
            public string message;
            public int next_step;
            public bool requires_user_action;
            public string pause_reason;
            public bool requires_explicit_user_choice;
            public string user_message;
            public string agent_instruction;
            public string[] available_actions;
        }
    }
}