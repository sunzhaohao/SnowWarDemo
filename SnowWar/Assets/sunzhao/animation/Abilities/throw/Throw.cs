using System.Collections;
using System.Collections.Generic;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using Opsive.UltimateCharacterController.Events;
using Opsive.UltimateCharacterController.Game;
using Opsive.UltimateCharacterController.Input;
using Opsive.UltimateCharacterController.SurfaceSystem;
using Opsive.UltimateCharacterController.Utility;


using UnityEngine;

[DefaultInputName("Throw")]
[DefaultStartType(AbilityStartType.ButtonDown)]
[DefaultStopType(AbilityStopType.Automatic)]
[DefaultAbilityIndex(102)]
[DefaultUseRootMotionPosition(AbilityBoolOverride.False)]
[DefaultUseRootMotionRotation(AbilityBoolOverride.False)]
public class Throw : Ability {

    [Tooltip("The number of seconds that the throw ability has to wait  before it can start again.")]
    [SerializeField] protected float m_RecurrenceDelay = 0.2f;

    private float m_LandTime = 0;

    public override bool CanStartAbility()
    {
        if (base.CanStartAbility() == false)
            return false;
        if (m_LandTime + m_RecurrenceDelay > Time.realtimeSinceStartup)
            return false;
      
        return true;
    }
    protected override void AbilityStarted()
    {
        m_LandTime = Time.realtimeSinceStartup;
        base.AbilityStarted();
    }
    /// <summary>
    /// Can the ability be stopped?
    /// </summary>
    /// <returns>True if the ability can be stopped.</returns>
    public override bool CanStopAbility()
    {
        if (Time.realtimeSinceStartup> m_LandTime+1.8)
            return true;
        
        return false;
    }

}
