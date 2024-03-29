using UnityEngine;

namespace Assets.Code.Items.Interfaces
{
    public interface IItem
    {
        string Name { get; }
        int MaxStack { get; }
        Sprite Icon { get; }
    }
}