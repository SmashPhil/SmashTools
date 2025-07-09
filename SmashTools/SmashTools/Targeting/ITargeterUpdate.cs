namespace SmashTools.Targeting;

public interface ITargeterUpdate<T>
{
  void TargeterOnGUI();

  void TargeterUpdate(ref readonly TargetData<T> targetData);
}