using Newtonsoft.Json;

namespace Anvil.API
{
  public sealed class NuiDrawListCircle : NuiDrawListItem
  {
    [JsonConstructor]
    public NuiDrawListCircle(NuiProperty<Color> color, NuiProperty<bool> fill, NuiProperty<float> lineThickness, NuiProperty<NuiRect> rect) : base(color, fill, lineThickness)
    {
      Rect = rect;
    }

    [JsonProperty("rect")]
    public NuiProperty<NuiRect> Rect { get; set; }

    public override NuiDrawListItemType Type => NuiDrawListItemType.Circle;
  }
}
