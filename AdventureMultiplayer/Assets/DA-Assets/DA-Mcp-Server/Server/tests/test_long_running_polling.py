from da_mcp_server.transport.models import ToolDefinitionModel


def test_polling_metadata_defaults():
    tool = ToolDefinitionModel(name="sample")

    assert tool.requiresPolling is False
    assert tool.pollAction == "status"
    assert tool.maxPollSeconds == 0
