using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace com.github.pandrabox.armatureconfigcreater.editor
{
    public class Creater
    {
        private static GameObject _targetObj;
        private static string TargetName => _targetObj.name;
        // Unity標準APIで親からコンポーネントを探す
        private static VRCAvatarDescriptor Descriptor => FindComponentInParent<VRCAvatarDescriptor>(_targetObj);
        private static GameObject ArmatureConfig => Descriptor.transform.GetComponentsInChildren<Transform>(true)?.FirstOrDefault(t => t.name == ARMATURECONFIGNAME)?.gameObject;
        private const string ARMATURECONFIGNAME = "ArmatureConfig";

        [MenuItem("GameObject/ArmatureConfig/Create", false, 20)]
        private static void CreateArmatureConfig(MenuCommand menuCommand)
        {
            _targetObj = Selection.activeGameObject;
            main();
        }
        [MenuItem("GameObject/ArmatureConfig/Remove", false, 20)]
        private static void RemoveArmatureConfigComponents(MenuCommand menuCommand)
        {
            _targetObj = Selection.activeGameObject;
            RemoveComponents();
        }
        [MenuItem("GameObject/ArmatureConfig/Remap", false, 20)]
        private static void RemapArmatureConfigComponents(MenuCommand menuCommand)
        {
            _targetObj = Selection.activeGameObject;
            ReferenceRemap();
        }

        private static void main()
        {
            if (Descriptor == null)
            {
                EditorUtility.DisplayDialog("処理をキャンセルしました", $@"指定したオブジェクト({TargetName})の親にAvatarDescriptorが見つかりません。この機能はArmatureを指定して実行してください。処理をキャンセルします。", "OK");
                return;
            }
            if (ArmatureConfig != null)
            {
                EditorUtility.DisplayDialog("処理をキャンセルしました", "ArmatureConfigは作成済みです。処理をキャンセルします。", "OK");
                return;
            }
            if (!TargetName.ToLower().Contains("armature"))
            {
                if (!EditorUtility.DisplayDialog("処理を続行しますか？", $@"指定したオブジェクト({TargetName})はArmatureでないかもしれません。本当に処理しますか？", "Yes", "No"))
                {
                    return;
                }
            }
            CreateArmatureConfig();
        }
        private static void CreateArmatureConfig()
        {
            var copiedObj = Object.Instantiate(_targetObj, Descriptor.transform);
            copiedObj.name = ARMATURECONFIGNAME;
            foreach (Transform child in copiedObj.GetComponentsInChildren<Transform>(true))
            {
                var components = child.GetComponents<Component>();
                foreach (var c in components)
                {
                    if (!(c is Transform || c is VRCPhysBone || c is VRCPhysBoneCollider))
                    {
                        Object.DestroyImmediate(c);
                    }
                }
            }
            var orgPath = new GameObject("OriginalPath");
            orgPath.transform.SetParent(copiedObj.transform);
            var pathObj = new GameObject(GetRelativePath(Descriptor.transform, _targetObj.transform).Replace("/", "@@"));
            pathObj.transform.SetParent(orgPath.transform);
            RemoveComponents(true);
            ReferenceRemap();
        }
        private static void RemoveComponents(bool beforeCheck = false)
        {
            if (beforeCheck)
            {
                var physBones = _targetObj.GetComponentsInChildren<VRCPhysBone>();
                if (physBones.Length == 0)
                {
                    var physBoneColliders = _targetObj.GetComponentsInChildren<VRCPhysBoneCollider>();
                    if (physBoneColliders.Length == 0)
                    {
                        //PBもPBCもない場合何もせず終了
                        return;
                    }
                }
                //オリジナルArmatureからVRCPhysBone,VRCphysBoneColliderを削除しますか？とメッセージで確認する
                if (!EditorUtility.DisplayDialog("処理を続行しますか？", "オリジナルArmatureからVRCPhysBone, VRCphysBoneColliderを削除しますか？\n\rメニュー：Removeによって後から実行することもできます。", "Yes", "No"))
                {
                    return;
                }
            }
            else
            {
                if (ArmatureConfig == null)
                {
                    EditorUtility.DisplayDialog("処理をキャンセルしました", "ArmatureConfigが見つからないため処理をキャンセルしました。\n\rこの機能はCreate時にオリジナルPB等を削除しなかった場合に後から実行するためのものです。", "OK");
                    return;
                }
                if (!EditorUtility.DisplayDialog("処理を続行しますか？", "オリジナルArmatureからVRCPhysBone, VRCphysBoneColliderを削除します。実行してよろしいですか？", "Yes", "No"))
                {
                    return;
                }
            }

            foreach (Transform child in _targetObj.GetComponentsInChildren<Transform>(true))
            {
                var components = child.GetComponents<Component>();
                foreach (var c in components)
                {
                    if ((c is VRCPhysBone || c is VRCPhysBoneCollider))
                    {
                        Object.DestroyImmediate(c);
                    }
                }
            }
        }

        private static void ReferenceRemap(bool NoMsg = false)
        {
            if (ArmatureConfig == null)
            {
                if (!NoMsg)
                {
                    EditorUtility.DisplayDialog("処理をキャンセルしました", "ArmatureConfigが見つからないため処理をキャンセルしました。\n\rこの機能はCreate後に何等かの理由でTargetTransformの参照が外れたときに修正するためのものです。", "OK");
                }
                return;
            }
            //ArmatureConfigの子のOriginalPathGameObjectの子のgameObjectの名前を取得
            string OriginalPath = ArmatureConfig?.transform?.Find("OriginalPath")?.GetChild(0)?.name?.Replace("@@", "/");
            Transform OriginalRootTransform = Descriptor?.transform?.Find(OriginalPath);
            if (!NoMsg && OriginalRootTransform == null)
            {
                EditorUtility.DisplayDialog("処理をキャンセルしました", "OriginalTransformが見つからないため処理をキャンセルしました。\n\r名前の変更などがあったかもしれません。再度Createすることを検討してください。", "OK");
                return;
            }

            foreach (Transform child in ArmatureConfig.GetComponentsInChildren<Transform>(true))
            {
                var components = child.GetComponents<Component>();
                foreach (var c in components)
                {
                    if (c is VRCPhysBone)
                    {
                        ((VRCPhysBone)c).rootTransform = getOriginalTransform(OriginalRootTransform, ArmatureConfig, c.gameObject);
                    }
                    if (c is VRCPhysBoneCollider)
                    {
                        ((VRCPhysBoneCollider)c).rootTransform = getOriginalTransform(OriginalRootTransform, ArmatureConfig, c.gameObject);
                    }
                    EditorUtility.SetDirty(c);
                }
            }
        }
        private static Transform getOriginalTransform(Transform originalRootTransform, GameObject armatureConfig, GameObject mirrorObject)
        {
            var relativePath = GetRelativePath(armatureConfig.transform, mirrorObject.transform);
            return originalRootTransform.Find(relativePath);
        }

        // Unity標準APIで親からコンポーネントを探す
        private static T FindComponentInParent<T>(GameObject obj) where T : Component
        {
            if (obj == null) return null;
            Transform current = obj.transform.parent;
            while (current != null)
            {
                T comp = current.GetComponent<T>();
                if (comp != null) return comp;
                current = current.parent;
            }
            return null;
        }

        // Unity標準APIでTransform間の相対パスを取得
        private static string GetRelativePath(Transform parent, Transform child)
        {
            if (parent == null || child == null) return null;
            if (!child.IsChildOf(parent)) return null;
            string path = "";
            Transform current = child;
            while (current != parent)
            {
                path = current.name + (path == "" ? "" : "/") + path;
                current = current.parent;
            }
            return path;
        }
    }
}