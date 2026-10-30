using Content.Shared.Mindshield.Components;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

#pragma warning disable IDE0130 // Пространство имён совпадает с исходным компонентом.
namespace Content.Shared.Mindshield.Components;

public sealed partial class MindShieldImplantComponent
{
    [DataField]
    public ProtoId<SecurityIconPrototype> MindShieldStatusIcon = "MindShieldIcon";
}