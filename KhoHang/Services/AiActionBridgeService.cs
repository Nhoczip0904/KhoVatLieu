using System;
using System.Collections.Generic;
using KhoHang.Models;

namespace KhoHang.Services;

public class AiActionBridgeService
{
    public event Action<AiActionMessage>? OnActionTriggered;

    public void TriggerAction(string actionType, Dictionary<string, string> data, List<AiActionMaterialDto> materials)
    {
        OnActionTriggered?.Invoke(new AiActionMessage
        {
            ActionType = actionType,
            Data = data,
            Materials = materials
        });
    }
}

public class AiActionMessage
{
    public string ActionType { get; set; } = string.Empty;
    public Dictionary<string, string> Data { get; set; } = new();
    public List<AiActionMaterialDto> Materials { get; set; } = new();
}

public class AiActionMaterialDto
{
    public string Name { get; set; } = string.Empty;
    public double Qty { get; set; }
    public decimal Price { get; set; }
}
