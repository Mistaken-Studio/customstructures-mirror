// -----------------------------------------------------------------------
// <copyright file="WarheadTimerHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using AdminToys;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Usables.Scp244;
using MEC;
using Mistaken.API.Components;
using Mistaken.UnityPrefabs;
using Mistaken.UnityPrefabs.SegmentDisplay;
using PlayableScps;
using UnityEngine;
using Utils.Networking;

namespace Mistaken.CustomStructures.AssetHandlers
{
    internal class TurretHandler : SingleAssetHandler
    {
        private InRangeBall range;

        private const float Range = 30f;
        private const float FireRate = 12;

        public override void Initialize(Asset asset)
        {
            base.Initialize(asset);
            this.script = this.gameObject.GetComponent<CameraLogicScript>();
            this.range = InRangeBall.Spawn(this.transform, Vector3.zero, Range + 5f, Range + 5f, null, (target) => this.script.toFollow = null);

            InvokeRepeating(nameof(UpdateTarget), 1, 1);
            InvokeRepeating(nameof(Shoot), 1, 1f / FireRate);

            gun = (Exiled.API.Features.Items.Item.Create(ItemType.GunLogicer) as Exiled.API.Features.Items.Firearm).Base;
            shootClipId = (byte)AttachmentsUtils.AttachmentsValue(gun, AttachmentParam.ShotClipIdOverride);
        }

        public override void OnDestroy()
        {
        }

        private void Shoot()
        {
            MakeSound();

            foreach (var item in this.script.GetComponentsInChildren<Collider>())
            {
                item.enabled = false;
            }

            try
            {
                var ray = new Ray(this.script.FollowObject.transform.position, this.script.FollowObject.transform.forward);
                FirearmBaseStats baseStats = new FirearmBaseStats
                {
                    BaseDamage = .5f,
                    BasePenetrationPercent = 100,
                    BulletInaccuracy = .5f,
                    AdsInaccuracy = 0.18f,
                    HipInaccuracy = 3.5f,
                    FullDamageDistance = Range + 10f,
                    DamageFalloff = 5f,
                };
                Vector3 a = (new Vector3(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value) - Vector3.one / 2f).normalized * UnityEngine.Random.value;
                float num = baseStats.GetInaccuracy(gun, false, 0, true);
                ray.direction = Quaternion.Euler(num * a) * ray.direction;
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, baseStats.MaxDistance(), StandardHitregBase.HitregMask))
                {
                    global::IDestructible destructible;
                    if (hit.collider.TryGetComponent<global::IDestructible>(out destructible))
                    {
                        float damage = baseStats.DamageAtDistance(gun, hit.distance);
                        if (destructible.Damage(damage, new PlayerStatsSystem.FirearmDamageHandler(gun, damage, true), hit.point))
                        {
                            global::ReferenceHub referenceHub;
                            if (!global::ReferenceHub.TryGetHubNetID(destructible.NetworkId, out referenceHub))
                            {
                                return;
                            }

                            foreach (global::ReferenceHub referenceHub2 in referenceHub.spectatorManager.ServerCurrentSpectatingPlayers)
                            {
                                referenceHub2.networkIdentity.connectionToClient.Send<InventorySystem.Items.Firearms.BasicMessages.GunHitMessage>(new InventorySystem.Items.Firearms.BasicMessages.GunHitMessage(false, damage, ray.origin), 0);
                            }

                            if (!global::ReferenceHub.TryGetHubNetID(destructible.NetworkId, out referenceHub) || !referenceHub.characterClassManager.IsHuman())
                            {
                                return;
                            }
                            new InventorySystem.Items.Firearms.BasicMessages.GunHitMessage(hit.point + (ray.origin - hit.point).normalized, ray.direction, true).SendToAuthenticated(0);
                        }
                    }
                    else
                    {
                        new InventorySystem.Items.Firearms.BasicMessages.GunHitMessage(hit.point + (ray.origin - hit.point).normalized, ray.direction, false).SendToAuthenticated(0);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error(ex);
            }

            foreach (var item in this.script.GetComponentsInChildren<Collider>())
            {
                item.enabled = true;
            }
        }

        private byte shootClipId;
        private InventorySystem.Items.Firearms.Firearm gun;

        private void MakeSound()
        {
            float loudLevel = 200f;

            float num2 = loudLevel * loudLevel;
            foreach (global::ReferenceHub referenceHub in global::ReferenceHub.GetAllHubs().Values)
            {
                global::RoleType curClass = referenceHub.characterClassManager.CurClass;
                if (curClass == global::RoleType.Spectator || curClass == global::RoleType.Scp079 || (referenceHub.transform.position - this.script.FollowObject.transform.position).sqrMagnitude <= num2)
                {
                    MirrorExtensions.PlayGunSound(Player.Get(referenceHub), this.script.FollowObject.transform.position, ItemType.GunLogicer, 255);
                }
            }
        }

        private void UpdateTarget()
        {
            if (!(this.script.toFollow is null))
                return;

            this.script.toFollow = this.range.ColliderInArea.FirstOrDefault(x => this.IsValidTarget(Player.Get(x)));
        }

        void LateUpdate()
        {

        }

        protected override AssetMeta.AssetType AssetType => AssetMeta.AssetType.TURRET_TEST;

        private CameraLogicScript script;

        public bool IsValidTarget(Player target)
        {
            if (!target.IsAlive)
                return false;

            var res = GetVisionInformation(this.script.FollowObject, target.Position + Vector3.up, -2f, Range, false, true, null, VisionInformation.VisionLayerMask);
            var reason = res.GetFailReason();
            Log.Debug(reason);
            return reason == VisionInformation.FailReason.IsLooking;
        }

        public static VisionInformation GetVisionInformation(GameObject source, Vector3 target, float targetRadius = 0f, float visionTriggerDistance = 0f, bool checkFog = true, bool checkLineOfSight = true, LocalCurrentRoomEffects targetLightCheck = null, int MaskLayer = 0)
        {
            Transform playerCameraReference = source.transform;
            bool isOnSameFloor = false;
            bool flag = false;
            if (Mathf.Abs(target.y - playerCameraReference.position.y) < 100f)
            {
                isOnSameFloor = true;
                flag = true;
            }

            bool flag2 = visionTriggerDistance == 0f;
            float num = 0f;
            Vector3 vector = target - playerCameraReference.position;
            if (flag && visionTriggerDistance > 0f)
            {
                float num2 = ((!checkFog) ? visionTriggerDistance : ((target.y > 980f) ? visionTriggerDistance : (visionTriggerDistance / 2f)));
                num = vector.magnitude;
                if (num <= num2)
                {
                    flag2 = true;
                }

                flag = flag2;
            }

            float lookingAmount = 1f;
            if (flag)
            {
                flag = false;
                if (num < Mathf.Abs(targetRadius))
                {
                    if (Vector3.Dot(source.transform.forward, (target - source.transform.position).normalized) > 0f)
                    {
                        flag = true;
                        lookingAmount = 1f;
                    }
                }
                else
                {
                    Vector3 vector2 = playerCameraReference.InverseTransformPoint(target);
                    if (targetRadius != 0f)
                    {
                        Vector2 vector3 = vector2.normalized * targetRadius;
                        vector2 = new Vector3(vector2.x + vector3.x, vector2.y + vector3.y, vector2.z);
                    }

                    float _num = Mathf.Tan(global::AspectRatioSync._defaultCameraFieldOfView * 0.0174532924f * 0.5f);
                    float XScreenEdge = Mathf.Atan(_num) * 57.29578f;
                    float XplusY = XScreenEdge + global::AspectRatioSync.YScreenEdge;

                    float num3 = Vector2.Angle(Vector2.up, new Vector2(vector2.x, vector2.z));
                    if (num3 < XScreenEdge)
                    {
                        float num4 = Vector2.Angle(Vector2.up, new Vector2(vector2.y, vector2.z));
                        if (num4 < global::AspectRatioSync.YScreenEdge)
                        {
                            lookingAmount = (num3 + num4) / XplusY;
                            flag = true;
                        }
                    }
                }
            }

            bool flag3 = !checkLineOfSight;
            if (flag && checkLineOfSight)
            {
                if (MaskLayer == 0)
                {
                    MaskLayer = VisionInformation.VisionLayerMask;
                }

                flag3 = Physics.RaycastNonAlloc(new Ray(playerCameraReference.position, vector.normalized), VisionInformation._raycastResult, flag2 ? num : vector.magnitude, MaskLayer) == 0;
                flag = flag3;
            }

            return new VisionInformation(null, target, flag, isOnSameFloor, lookingAmount, num, flag2, false, flag3);
        }
    }
}
