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
        // Unity�W��API�Őe����R���|�[�l���g��T��
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
                EditorUtility.DisplayDialog("�������L�����Z�����܂���", $@"�w�肵���I�u�W�F�N�g({TargetName})�̐e��AvatarDescriptor��������܂���B���̋@�\��Armature���w�肵�Ď��s���Ă��������B�������L�����Z�����܂��B", "OK");
                return;
            }
            if (ArmatureConfig != null)
            {
                EditorUtility.DisplayDialog("�������L�����Z�����܂���", "ArmatureConfig�͍쐬�ς݂ł��B�������L�����Z�����܂��B", "OK");
                return;
            }
            if (!TargetName.ToLower().Contains("armature"))
            {
                if (!EditorUtility.DisplayDialog("�����𑱍s���܂����H", $@"�w�肵���I�u�W�F�N�g({TargetName})��Armature�łȂ���������܂���B�{���ɏ������܂����H", "Yes", "No"))
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
                        //PB��PBC���Ȃ��ꍇ���������I��
                        return;
                    }
                }
                //�I���W�i��Armature����VRCPhysBone,VRCphysBoneCollider���폜���܂����H�ƃ��b�Z�[�W�Ŋm�F����
                if (!EditorUtility.DisplayDialog("�����𑱍s���܂����H", "�I���W�i��Armature����VRCPhysBone, VRCphysBoneCollider���폜���܂����H\n\r���j���[�FRemove�ɂ���Čォ����s���邱�Ƃ��ł��܂��B", "Yes", "No"))
                {
                    return;
                }
            }
            else
            {
                if (ArmatureConfig == null)
                {
                    EditorUtility.DisplayDialog("�������L�����Z�����܂���", "ArmatureConfig��������Ȃ����ߏ������L�����Z�����܂����B\n\r���̋@�\��Create���ɃI���W�i��PB�����폜���Ȃ������ꍇ�Ɍォ����s���邽�߂̂��̂ł��B", "OK");
                    return;
                }
                if (!EditorUtility.DisplayDialog("�����𑱍s���܂����H", "�I���W�i��Armature����VRCPhysBone, VRCphysBoneCollider���폜���܂��B���s���Ă�낵���ł����H", "Yes", "No"))
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
                    EditorUtility.DisplayDialog("�������L�����Z�����܂���", "ArmatureConfig��������Ȃ����ߏ������L�����Z�����܂����B\n\r���̋@�\��Create��ɉ������̗��R��TargetTransform�̎Q�Ƃ��O�ꂽ�Ƃ��ɏC�����邽�߂̂��̂ł��B", "OK");
                }
                return;
            }
            //ArmatureConfig�̎q��OriginalPathGameObject�̎q��gameObject�̖��O���擾
            string OriginalPath = ArmatureConfig?.transform?.Find("OriginalPath")?.GetChild(0)?.name?.Replace("@@", "/");
            Transform OriginalRootTransform = Descriptor?.transform?.Find(OriginalPath);
            if (!NoMsg && OriginalRootTransform == null)
            {
                EditorUtility.DisplayDialog("�������L�����Z�����܂���", "OriginalTransform��������Ȃ����ߏ������L�����Z�����܂����B\n\r���O�̕ύX�Ȃǂ���������������܂���B�ēxCreate���邱�Ƃ��������Ă��������B", "OK");
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

        // Unity�W��API�Őe����R���|�[�l���g��T��
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

        // Unity�W��API��Transform�Ԃ̑��΃p�X���擾
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