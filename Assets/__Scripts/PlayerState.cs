using System;
using UnityEngine;

// bitfield for disabled controls
 [Flags]
 public enum DisabledControls
 {
    None,
    LookHorizontal = 1,
    LookVertical = 2,
    MoveLeft = 4,
    MoveRight = 8,
    MoveUp = 16,
    MoveDown = 32,
    Jump = 64,
    Interact = 128,
    Attack = 256,
    All = -1
 };

[Serializable]
public class PlayerState
{
    public string playerName = "Player";
    //public int playerHealth = 100;

    public DisabledControls currentlyDisabledControls = DisabledControls.None;
}
