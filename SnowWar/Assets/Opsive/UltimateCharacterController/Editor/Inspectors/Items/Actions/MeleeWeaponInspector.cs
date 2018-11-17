/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using Opsive.UltimateCharacterController.Items.Actions;
using Opsive.UltimateCharacterController.Editor.Inspectors.Items.AnimatorAudioState;
using Opsive.UltimateCharacterController.Editor.Inspectors.StateSystem;
using Opsive.UltimateCharacterController.Editor.Inspectors.Utility;
using System;
using System.Collections.Generic;

namespace Opsive.UltimateCharacterController.Editor.Inspectors.Items.Actions
{
    /// <summary>
    /// Shows a custom inspector for the MeleeWeapon component.
    /// </summary>
    [CustomEditor(typeof(MeleeWeapon))]
    public class MeleeWeaponInspector : UsableItemInspector
    {
        private const string c_EditorPrefsSelectedRecoilAnimatorAudioStateSetIndexKey = "Opsive.UltimateCharacterController.Editor.Inspectors.SelectedRecoilAnimatorAudioStateSetIndex";
        private const string c_EditorPrefsSelectedRecoilAnimatorAudioStateSetStateIndexKey = "Opsive.UltimateCharacterController.Editor.Inspectors.SelectedRecoilAnimatorAudioStateSetStateIndex";
        private string SelectedRecoilAnimatorAudioStateSetIndexKey { get { return c_EditorPrefsSelectedRecoilAnimatorAudioStateSetIndexKey + "." + target.GetType() + "." + target.name; } }

        private MeleeWeapon m_MeleeWeapon;
        private ReorderableList m_ReorderableRecoilAnimatorAudioStateSetList;
        private ReorderableList m_ReorderableRecoilAnimatorAudioStateSetAudioList;
        private ReorderableList m_ReorderableRecoilAnimatorAudioStateSetStateList;

        /// <summary>
        /// Initialize any starting values.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            m_MeleeWeapon = target as MeleeWeapon;
            m_MeleeWeapon.RecoilAnimatorAudioStateSet.DeserializeAnimatorAudioStateSelector(m_Item, m_Item.GetComponentInParent<UltimateCharacterController.Character.UltimateCharacterLocomotion>());
        }

        /// <summary>
        /// Returns the actions to draw before the State list is drawn.
        /// </summary>
        /// <returns>The actions to draw before the State list is drawn.</returns>
        protected override Action GetDrawCallback()
        {
            var baseCallback = base.GetDrawCallback();

            baseCallback += () =>
            {
                if (Foldout("Melee")) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(PropertyFromName("m_RequireInAirMeleeAbilityInAir"));
                    EditorGUILayout.PropertyField(PropertyFromName("m_ConsumableItemType"));
                    EditorGUILayout.PropertyField(PropertyFromName("m_AimItemSubstateIndexAddition"));
                    EditorGUILayout.PropertyField(PropertyFromName("m_MaxCollisionCount"));
                    EditorGUILayout.PropertyField(PropertyFromName("m_SingleHit"));
                    EditorGUILayout.PropertyField(PropertyFromName("m_CanHitDelay"));
                    EditorGUILayout.PropertyField(PropertyFromName("m_AllowAttackCombos"));
                    EditorGUI.indentLevel--;
                }

                if (Foldout("Impact")) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(PropertyFromName("m_ImpactLayers"));
                    EditorGUILayout.PropertyField(PropertyFromName("m_DamageAmount"));
                    EditorGUILayout.PropertyField(PropertyFromName("m_ImpactForce"));
                    EditorGUILayout.PropertyField(PropertyFromName("m_ImpactForceFrames"));
                    EditorGUILayout.PropertyField(PropertyFromName("m_ImpactStateName"));
                    EditorGUILayout.PropertyField(PropertyFromName("m_ImpactStateDisableTimer"));
                    EditorGUILayout.PropertyField(PropertyFromName("m_SurfaceImpact"));
                    EditorGUI.indentLevel--;
                }

                if (Foldout("Recoil")) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(PropertyFromName("m_ApplyRecoil"));
                    if (Foldout("Animator Audio")) {
                        EditorGUI.indentLevel++;
                        AnimatorAudioStateSetInspector.DrawAnimatorAudioStateSet(m_MeleeWeapon, m_MeleeWeapon.RecoilAnimatorAudioStateSet, false,
                                    ref m_ReorderableRecoilAnimatorAudioStateSetList, OnAnimatorAudioStateListDraw, OnAnimatorAudioStateListSelect,
                                    OnAnimatorAudioStateListAdd, OnAnimatorAudioStateListRemove, SelectedRecoilAnimatorAudioStateSetIndexKey,
                                    ref m_ReorderableRecoilAnimatorAudioStateSetAudioList, OnRecoilAudioListElementDraw, OnRecoilAudioListAdd, OnRecoilAudioListRemove, 
                                    ref m_ReorderableRecoilAnimatorAudioStateSetStateList, OnAnimatorAudioStateSetStateListDraw, OnAnimatorAudioStateSetStateListAdd,
                                    OnAnimatorAudioStateSetStateListReorder, OnAnimatorAudioStateListRemove,
                                    GetSelectedAnimatorAudioStateSetStateIndexKey(EditorPrefs.GetInt(SelectedRecoilAnimatorAudioStateSetIndexKey)));
                    }
                    EditorGUI.indentLevel--;
                }

                if (Foldout("Trail")) {
                    EditorGUI.indentLevel++;
                    var trail = PropertyFromName("m_Trail");
                    EditorGUILayout.PropertyField(trail);
                    if (trail.objectReferenceValue != null) {
                        EditorGUILayout.PropertyField(PropertyFromName("m_TrailVisibility"));
                        EditorGUILayout.PropertyField(PropertyFromName("m_TrailSpawnDelay"));
                        InspectorUtility.DrawAnimationEventTrigger(target, "Attack Stop Trail Event", PropertyFromName("m_AttackStopTrailEvent"));
                    }
                    EditorGUI.indentLevel--;
                }

                if (Foldout("Events")) {
                    EditorGUI.indentLevel++;
                    InspectorUtility.UnityEventPropertyField(PropertyFromName("m_OnImpactEvent"));
                    EditorGUI.indentLevel--;
                }
            };

            return baseCallback;
        }

        /// <summary>
        /// Draws all of the added audio clip elements.
        /// </summary>
        private void OnRecoilAudioListElementDraw(Rect rect, int index, bool isActive, bool isFocused)
        {
            AnimatorAudioStateSetInspector.OnAudioClipDraw(rect, index, m_MeleeWeapon.RecoilAnimatorAudioStateSet.States, SelectedRecoilAnimatorAudioStateSetIndexKey, target);
        }

        /// <summary>
        /// Adds a new audio clip element to the list.
        /// </summary>
        private void OnRecoilAudioListAdd(ReorderableList list)
        {
            AnimatorAudioStateSetInspector.OnAudioClipListAdd(list, m_MeleeWeapon.RecoilAnimatorAudioStateSet.States, SelectedRecoilAnimatorAudioStateSetIndexKey, target);
        }

        /// <summary>
        /// Removes the audio clip at the list index.
        /// </summary>
        private void OnRecoilAudioListRemove(ReorderableList list)
        {
            AnimatorAudioStateSetInspector.OnAudioClipListRemove(list, m_MeleeWeapon.RecoilAnimatorAudioStateSet.States, SelectedRecoilAnimatorAudioStateSetIndexKey, target);
        }

        /// <summary>
        /// Draws the AudioStateSet element.
        /// </summary>
        private void OnAnimatorAudioStateListDraw(Rect rect, int index, bool isActive, bool isFocused)
        {
            AnimatorAudioStateSetInspector.OnAnimatorAudioStateElementDraw(m_MeleeWeapon.RecoilAnimatorAudioStateSet, rect, index, isActive, isFocused, target);
        }

        /// <summary>
        /// A new element has been selected.
        /// </summary>
        private void OnAnimatorAudioStateListSelect(ReorderableList list)
        {
            AnimatorAudioStateSetInspector.OnAnimatorAudioStateSelect(list, SelectedRecoilAnimatorAudioStateSetIndexKey);
        }

        /// <summary>
        /// Adds a new state element to the list.
        /// </summary>
        private void OnAnimatorAudioStateListAdd(ReorderableList list)
        {
            AnimatorAudioStateSetInspector.OnAnimatorAudioStateListAdd(list, m_MeleeWeapon.RecoilAnimatorAudioStateSet, SelectedRecoilAnimatorAudioStateSetIndexKey);
        }

        /// <summary>
        /// Removes the state at the list index.
        /// </summary>
        private void OnAnimatorAudioStateListRemove(ReorderableList list)
        {
            AnimatorAudioStateSetInspector.OnAnimatorAudioStateListRemove(list, m_MeleeWeapon.RecoilAnimatorAudioStateSet, SelectedRecoilAnimatorAudioStateSetIndexKey);
        }

        /// <summary>
        /// Returns the state index key for the specified AnimatorAudioStateSet type.
        /// </summary>
        private string GetSelectedAnimatorAudioStateSetStateIndexKey(int index)
        {
            return c_EditorPrefsSelectedRecoilAnimatorAudioStateSetStateIndexKey + "." + target.GetType() + "." + target.name + "." + index;
        }

        /// <summary>
        /// Draws all of the added states.
        /// </summary>
        private void OnAnimatorAudioStateSetStateListDraw(Rect rect, int index, bool isActive, bool isFocused)
        {
            EditorGUI.BeginChangeCheck();
            var animatorAudioState = m_MeleeWeapon.RecoilAnimatorAudioStateSet.States[EditorPrefs.GetInt(SelectedRecoilAnimatorAudioStateSetIndexKey)];

            // The index may be out of range if the component was copied.
            if (index >= animatorAudioState.States.Length) {
                m_ReorderableRecoilAnimatorAudioStateSetStateList.index = -1;
                EditorPrefs.SetInt(GetSelectedAnimatorAudioStateSetStateIndexKey(EditorPrefs.GetInt(SelectedRecoilAnimatorAudioStateSetIndexKey)), m_ReorderableRecoilAnimatorAudioStateSetStateList.index);
                return;
            }

            StateInspector.OnStateListDraw(animatorAudioState, animatorAudioState.States, rect, index);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(target, "Change Value");
                StateInspector.UpdateDefaultStateValues(animatorAudioState.States);
            }
        }

        /// <summary>
        /// Adds a new state element to the list.
        /// </summary>
        private void OnAnimatorAudioStateSetStateListAdd(ReorderableList list)
        {
            StateInspector.OnStateListAdd(AddExistingAnimatorAudioStateSetStatePreset, CreateAnimatorAudioStateSetStatePreset);
        }

        /// <summary>
        /// Adds a new element to the state list which uses an existing preset.
        /// </summary>
        private void AddExistingAnimatorAudioStateSetStatePreset()
        {
            var animatorAudioState = m_MeleeWeapon.RecoilAnimatorAudioStateSet.States[EditorPrefs.GetInt(SelectedRecoilAnimatorAudioStateSetIndexKey)];
            var states = StateInspector.AddExistingPreset(animatorAudioState.GetType(), animatorAudioState.States, m_ReorderableRecoilAnimatorAudioStateSetStateList, GetSelectedAnimatorAudioStateSetStateIndexKey(EditorPrefs.GetInt(SelectedRecoilAnimatorAudioStateSetIndexKey)));
            if (animatorAudioState.States.Length != states.Length) {
                InspectorUtility.SynchronizePropertyCount(states, m_ReorderableRecoilAnimatorAudioStateSetStateList.serializedProperty);
                Undo.RecordObject(target, "Change Value");
                animatorAudioState.States = states;
            }
        }

        /// <summary>
        /// Creates a new preset and adds it to a new state in the list.
        /// </summary>
        private void CreateAnimatorAudioStateSetStatePreset()
        {
            var animatorAudioState = m_MeleeWeapon.RecoilAnimatorAudioStateSet.States[EditorPrefs.GetInt(SelectedRecoilAnimatorAudioStateSetIndexKey)];
            var states = StateInspector.CreatePreset(animatorAudioState, animatorAudioState.States, m_ReorderableRecoilAnimatorAudioStateSetStateList, GetSelectedAnimatorAudioStateSetStateIndexKey(EditorPrefs.GetInt(SelectedRecoilAnimatorAudioStateSetIndexKey)));
            if (animatorAudioState.States.Length != states.Length) {
                InspectorUtility.SynchronizePropertyCount(states, m_ReorderableRecoilAnimatorAudioStateSetStateList.serializedProperty);
                Undo.RecordObject(target, "Change Value");
                animatorAudioState.States = states;
            }
        }

        /// <summary>
        /// The list has been reordered. Ensure the reorder is valid.
        /// </summary>
        private void OnAnimatorAudioStateSetStateListReorder(ReorderableList list)
        {
            var animatorAudioState = m_MeleeWeapon.RecoilAnimatorAudioStateSet.States[EditorPrefs.GetInt(SelectedRecoilAnimatorAudioStateSetIndexKey)];

            // Use the dummy array in order to determine what element the selected index was swapped with.
            var copiedStates = new UltimateCharacterController.StateSystem.State[animatorAudioState.States.Length];
            Array.Copy(animatorAudioState.States, copiedStates, animatorAudioState.States.Length);
            for (int i = 0; i < animatorAudioState.States.Length; ++i) {
                var element = list.serializedProperty.GetArrayElementAtIndex(i);
                if (element.intValue != i) {
                    animatorAudioState.States[i] = copiedStates[element.intValue];
                    element.intValue = i;
                }
            }

            var states = StateInspector.OnStateListReorder(animatorAudioState.States);
            if (animatorAudioState.States.Length != states.Length) {
                InspectorUtility.SynchronizePropertyCount(states, m_ReorderableRecoilAnimatorAudioStateSetStateList.serializedProperty);
                Undo.RecordObject(target, "Change Value");
                animatorAudioState.States = states;
            }
        }

        /// <summary>
        /// The ReordableList remove button has been pressed. Remove the selected state.
        /// </summary>
        private void OnAnimatorAudioStateSetStateListRemove(ReorderableList list)
        {
            var animatorAudioState = m_MeleeWeapon.RecoilAnimatorAudioStateSet.States[EditorPrefs.GetInt(SelectedRecoilAnimatorAudioStateSetIndexKey)];
            var states = StateInspector.OnStateListRemove(animatorAudioState.States, GetSelectedAnimatorAudioStateSetStateIndexKey(EditorPrefs.GetInt(SelectedRecoilAnimatorAudioStateSetIndexKey)), list);
            if (animatorAudioState.States.Length != states.Length) {
                InspectorUtility.SynchronizePropertyCount(states, m_ReorderableRecoilAnimatorAudioStateSetStateList.serializedProperty);
                Undo.RecordObject(target, "Change Value");
                animatorAudioState.States = states;
            }
        }

        /// <summary>
        /// Deserialize the animator audio state set after an undo/redo.
        /// </summary>
        protected override void OnUndoRedo()
        {
            m_MeleeWeapon.RecoilAnimatorAudioStateSet.DeserializeAnimatorAudioStateSelector(m_Item, m_Item.GetComponentInParent<UltimateCharacterController.Character.UltimateCharacterLocomotion>());

            base.OnUndoRedo();
        }

        /// <summary>
        /// Adds a new hitbox to the list.
        /// </summary>
        public static void OnHitboxListAdd(IMeleeWeaponPerspectiveProperties meleePerspective, ReorderableList list, string selectedKey)
        {
            var hitboxes = meleePerspective.Hitboxes;
            if (hitboxes == null) {
                hitboxes = new MeleeWeapon.MeleeHitbox[1];
            } else {
                Array.Resize(ref hitboxes, hitboxes.Length + 1);
            }

            var hitbox = new MeleeWeapon.MeleeHitbox();
            hitboxes[hitboxes.Length - 1] = hitbox;
            meleePerspective.Hitboxes = hitboxes;

            // Select the newly added hitbox.
            list.list = meleePerspective.Hitboxes;
            list.index = hitboxes.Length - 1;
            EditorPrefs.SetInt(selectedKey, list.index);
            InspectorUtility.SetDirty(meleePerspective as UnityEngine.Object);
        }

        /// <summary>
        /// Remove the hitbox at the list index.
        /// </summary>
        public static void OnHitboxListRemove(IMeleeWeaponPerspectiveProperties meleePerspective, ReorderableList list, string selectedKey)
        {
            var hitboxes = new List<MeleeWeapon.MeleeHitbox>(meleePerspective.Hitboxes);

            // Remove the element.
            Undo.RecordObject(meleePerspective as UnityEngine.Object, "Change Value");
            hitboxes.RemoveAt(list.index);
            list.list = meleePerspective.Hitboxes = hitboxes.ToArray();

            // Update the index to point to no longer point to the now deleted view type.
            list.index = list.index - 1;
            if (list.index == -1 && hitboxes.Count > 0) {
                list.index = 0;
            }
            EditorPrefs.SetInt(selectedKey, list.index);
            InspectorUtility.SetDirty(meleePerspective as UnityEngine.Object);
        }

        /// <summary>
        /// A new element has been selected within the list.
        /// </summary>
        public static void OnHitboxListSelect(ref ReorderableList list, string selectedKey)
        {
            EditorPrefs.SetInt(selectedKey, list.index);
            // The list should start out fresh so a reference doesn't have to be cached for each hitbox.
            list = null;
        }

        /// <summary>
        /// Draws the specified hitbox.
        /// </summary>
        public static void DrawSelectedHitbox(IMeleeWeaponPerspectiveProperties meleePerspective, object hitbox)
        {
            EditorGUI.BeginChangeCheck();

            ObjectInspector.DrawFields(hitbox, false);

            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(meleePerspective as UnityEngine.Object, "Change Value");
                InspectorUtility.SetDirty(meleePerspective as UnityEngine.Object);
            }
        }
    }
}