﻿using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MonoMod.Utils;
using MonoMod;
using HutongGames;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using ModCommon;
using ModCommon.Util;
using GlobalEnums;
using Object = UnityEngine.Object;


namespace HollowPoint
{
    class HPControl : MonoBehaviour
    {
        private GameObject fireball;
        private GameObject gunSpriteGO;
        private PlayMakerFSM fireballFSM;
        private PlayMakerFSM fireballControlFSM;
        private GunSpriteRenderer gunSpriteControl;
        private readonly System.Random recoilNum = new System.Random();
        private float recoilVal;
        private Vector3 defaultWeaponPos = new Vector3(-0.2f, -0.81f, -0.0001f);


        AttackDirection ad;

        //INTIALIZATION
        public void Awake()
        {

        }

        public void Start()
        {
            ModHooks.Instance.ObjectPoolSpawnHook += BooletSize;
            ModHooks.Instance.AttackHook += Attack_Hook;
            On.NailSlash.StartSlash += Start_Slash;
            On.HealthManager.Hit += spellDam;

            StartCoroutine(InitializationCoroutine());
        }


        //IF POSSIBLE MOVE ALL THE WEAPON RENDERER STUFF ON ANOTHER CLASS
        public IEnumerator InitializationCoroutine()
        {
            do
            {
                yield return null;
            }
            while (HeroController.instance == null || GameManager.instance == null || HeroController.instance.spellControl == null);
            
            gunSpriteGO = new GameObject("HollowPointGunSprite", typeof(SpriteRenderer), typeof(GunSpriteRenderer));
            gunSpriteGO.transform.parent = HeroController.instance.spellControl.gameObject.transform;
            gunSpriteGO.transform.localPosition = new Vector3(-0.2f, -0.81f, -0.0001f);
            gunSpriteGO.SetActive(true);
            Modding.Logger.Log("[HOLLOW POINT] HPControl.cs sucessfully initialized!");
        }

        #region Temporary Weapon Rotation
        //Give the weapon a shake when moving
        public void Update()
        {

            if (HeroController.instance.cState.wallSliding || (HeroController.instance.cState.superDashOnWall))
            {
                gunSpriteGO.transform.localPosition = defaultWeaponPos - new Vector3(-0.2f, 0.1f, 0f);
                return;
            }
            if (HeroController.instance.cState.superDashing)
            {
                gunSpriteGO.transform.localPosition = defaultWeaponPos - new Vector3(0f, 0.4f, 0f);
                return;
            }

            if (AmmunitionControl.firing)
            {
                gunSpriteGO.transform.SetRotationZ(0);
                gunSpriteGO.transform.localPosition = defaultWeaponPos;
            }
            else if (AmmunitionControl.reloading)
            {
                gunSpriteGO.transform.SetRotationZ(45);
                gunSpriteGO.transform.localPosition = defaultWeaponPos - new Vector3(0f, 0.1f, 0f);
            }
            else if (HeroController.instance.hero_state == ActorStates.running)
            {
                gunSpriteGO.transform.SetRotationZ(15);
                gunSpriteGO.transform.localPosition = defaultWeaponPos - new Vector3(0f, 0.1f, 0f);
            }
            else if (HeroController.instance.hero_state == ActorStates.airborne)
            {
                gunSpriteGO.transform.SetRotationZ(20);
                gunSpriteGO.transform.localPosition = defaultWeaponPos - new Vector3(0f, 0.1f, 0f);
            }
            else
            {
                gunSpriteGO.transform.SetRotationZ(0);
                gunSpriteGO.transform.localPosition = defaultWeaponPos;
            }


        }
        #endregion 

        //Modify the damage
        public void spellDam(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
        {
            if (hitInstance.AttackType == AttackTypes.Spell && hitInstance.Source.name.StartsWith("bullet"))
            {
                BulletBehavior b;
                Log("Bullet impact with name " + hitInstance.Source.name);
                
                b = fireball.GetComponent<BulletBehavior>();
                /*
                if (!b.enemyHit())
                    return;
                if (self.IsBlockingByDirection(
                    DirectionUtils.GetCardinalDirection(hitInstance.GetActualDirection(self.transform)),
                    hitInstance.AttackType))
                {
                    orig(self, hitInstance);
                    return;
                }
                */
                DamageEnemies.hitEnemy(self, b.bulletType.Damage, hitInstance, b.bulletType.SoulGain);
            }
            else
            {
                orig(self, hitInstance);
            }
        }

        //SHOOT/FIRE METHOD
        public void Attack_Hook(AttackDirection ad)
        {
            this.ad = ad;
        }

        public void Start_Slash(On.NailSlash.orig_StartSlash orig, NailSlash self)
        {
            if ((ad != AttackDirection.downward && !AmmunitionControl.reloading) && !AmmunitionControl.currAmmoType.AmmoName.Contains("Nail"))
            {
                //This coroutine starts a loop that will continue until the fireball object is instantiated, once it is it can then alter the fireball fsm if needed
                StartCoroutine(CheckIfNull(HeroController.instance.cState.facingRight));
                AmmunitionControl.firing = true;
                if (AmmunitionControl.currAmmoType.AmmoName.Contains("9mm"))
                {
                    StartCoroutine(BurstFire(5));
                }
                else if (AmmunitionControl.currAmmoType.AmmoName.Contains("5.56"))
                {
                    StartCoroutine(BurstFire(3));
                }
                else if (AmmunitionControl.currAmmoType.AmmoName.Contains("Gauge"))
                {
                    StartCoroutine(ShotgunFire(5));
                }
                else
                {
                    PlaySound(); 
                    AmmunitionControl.currAmmoType.CurrAmmo--;
                    AmmunitionControl.firing = false;
                    Schutz(ad);
                }

                if (AmmunitionControl.currAmmoType.CurrAmmo <= 0)
                {
                    Modding.Logger.Log("START RELOADING NOW");
                    AmmunitionControl.reloading = true;
                }
            }
            else
            {
                orig(self);
            }
        }

        public IEnumerator BurstFire(int x)
        {
            for(int i = x; i > 0 && AmmunitionControl.currAmmoType.CurrAmmo > 0; i--)
            {
                Schutz(ad);
                PlaySound();
                AmmunitionControl.currAmmoType.CurrAmmo--;
                yield return new WaitForSeconds(0.10f);
            }

            AmmunitionControl.firing = false;
            yield return null;
        }

        public IEnumerator ShotgunFire(int x)
        {
            PlaySound();
            for (int i = x; i > 0; i--)
            {
                Schutz(ad);
            }
            AmmunitionControl.firing = false;
            AmmunitionControl.currAmmoType.CurrAmmo--;
            yield return null;
        }

        public void PlaySound()
        {
            HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.bulletSoundFX[AmmunitionControl.currAmmoIndex - 1]);
        }

        public void Schutz(AttackDirection aDir)
        { 

            fireball = Instantiate(HeroController.instance.spell1Prefab, HeroController.instance.transform.position - new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0));

            fireball.transform.localScale = new Vector3(1f, 0.1f, 0.01f);

            fireballFSM = fireball.LocateMyFSM("Fireball Cast");

            fireballFSM.FsmVariables.GetFsmFloat("Fire Speed").Value = AmmunitionControl.currAmmoType.BulletVelocity;

            // Destroy the old camera shake actions and replace with a simple small shake.
            /*
            FsmState fbState = fireballFSM.GetState("Cast Right");
            fbState.Actions = fbState.Actions.Where(action => !(action is SendEventByName)).ToArray();
            fbState = fireballFSM.GetState("Cast Left");
            fbState.Actions = fbState.Actions.Where(action => !(action is SendEventByName)).ToArray();
            */ 
             
            // Shake screen
            GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");

            //This block removes the default audio and wall hit shake

            //Shooting toward the right
            if (HeroController.instance.cState.facingRight)
            {
                fireball.transform.position += new Vector3(0.80f, -0.8f, 0f);
                fireballFSM.GetAction<SendEventByName>("Cast Right", 1).sendEvent = "";
                fireballFSM.GetAction<AudioPlayerOneShotSingle>("Cast Right", 6).volume = 0;
                fireballFSM.GetAction<SpawnObjectFromGlobalPool>("Cast Right", 7).position = new Vector3(0, 0, 0);

                //Deviation/Recoil
                RecoilIncrease();
                recoilVal = recoilNum.Next(-AmmunitionControl.currAmmoType.CurrRecoilDeviation, AmmunitionControl.currAmmoType.CurrRecoilDeviation);
                fireball.transform.Rotate(new Vector3(0, 0, recoilVal + FireAtDiagonal()));
                fireballFSM.GetAction<SetVelocityAsAngle>("Cast Right", 9).angle = CheckIfRightAngle(0 + recoilVal + FireAtDiagonal());
            }
            //Shooting toward the left
            else if (!HeroController.instance.cState.facingRight)
            {
                fireball.transform.position += new Vector3(-0.80f, -0.8f, 0f);
                fireballFSM.GetAction<SendEventByName>("Cast Left", 1).sendEvent = "";
                fireballFSM.GetAction<AudioPlayerOneShotSingle>("Cast Left", 3).volume = 0;
                fireballFSM.GetAction<SpawnObjectFromGlobalPool>("Cast Left", 4).position = new Vector3(0, 0, 0);

                //Deviation/Recoil
                RecoilIncrease();
                recoilVal = recoilNum.Next(-AmmunitionControl.currAmmoType.CurrRecoilDeviation, AmmunitionControl.currAmmoType.CurrRecoilDeviation);
                fireball.transform.Rotate(new Vector3(0, 0, recoilVal - FireAtDiagonal()));
                fireballFSM.GetAction<SetVelocityAsAngle>("Cast Left", 6).angle = CheckIfRightAngle(180 + recoilVal - FireAtDiagonal());
            }

            //Destroy(fireball, 0.25f);
        }

        public float FireAtDiagonal()
        {
            //SHOOT UP AND RIGHT
            if (InputHandler.Instance.inputActions.right.IsPressed && InputHandler.Instance.inputActions.up.IsPressed)
            {
                Log("UP AND RIGHT");
                return 45;
            }
            //SHOOT UP AND LEFT
            else if (InputHandler.Instance.inputActions.left.IsPressed && InputHandler.Instance.inputActions.up.IsPressed)
            {
                return 45;
            }
            else if (InputHandler.Instance.inputActions.up.IsPressed)
            {
                return 89f;
            }
            return 0;
        }

        public void RecoilIncrease()
        {
            if(AmmunitionControl.currAmmoType.MaxDegreeDeviation <= AmmunitionControl.currAmmoType.CurrRecoilDeviation)
            {
                return;
            }

            //Should i turn this into a case switch instead???
            if (AmmunitionControl.currAmmoType.AmmoName.Contains("9mm"))
            {
                AmmunitionControl.currAmmoType.CurrRecoilDeviation += 2;
            }          
            else if (AmmunitionControl.currAmmoType.AmmoName.Contains("12 Gauge"))
            {
                AmmunitionControl.currAmmoType.CurrRecoilDeviation = AmmunitionControl.currAmmoType.MaxDegreeDeviation; //because why would a shotgun increase recoil as you keep firing it?
            }
            else if (AmmunitionControl.currAmmoType.AmmoName.Contains("7.62"))
            {
                AmmunitionControl.currAmmoType.CurrRecoilDeviation = 0;
            }
            else
            {
                AmmunitionControl.currAmmoType.CurrRecoilDeviation += 2;
            }
        }

        public float CheckIfRightAngle(float rv)
        {
            if(rv == 90)
            {
                if (HeroController.instance.cState.facingRight)
                {
                    return rv = 89f; //Spawning fireballs at a 90 degree angle destroys the go on cast, for some reason
                }
                else if (!HeroController.instance.cState.facingRight)
                {
                    return rv = 91f;
                }
            }

            return rv;
        }

        public IEnumerator CheckIfNull(bool facingRight)
        {
            do
            {
                yield return null;
            }
            while (fireballFSM.FsmVariables.GetFsmGameObject("Fireball").Value.LocateMyFSM("Fireball Control") == null);

            fireballControlFSM = fireballFSM.FsmVariables.GetFsmGameObject("Fireball").Value.LocateMyFSM("Fireball Control");
            fireballControlFSM.GetAction<SendEventByName>("Wall Impact", 2).sendEvent = "";

            yield return null;
        }

        //BULLET SIZE CHANGES
        public GameObject BooletSize(GameObject go)
        {
            if (go.name.Contains("Fireball") || go.name.StartsWith("bullet"))
            {
                //Let the 2nd parameter of float be the distance the bullet can travel before it dissipates, this can then be used as a bullet range
                //Destroy(go, 0.23f);
                StartCoroutine(ShrinkBooletSize(go));
            }
            return go;
        }

        public IEnumerator ShrinkBooletSize(GameObject go)
        {
            yield return new WaitForEndOfFrame();
            go.GetComponent<Transform>().localScale = AmmunitionControl.currAmmoType.BulletSize;
            go.LocateMyFSM("Fireball Control").GetAction<SendEventByName>("Wall Impact", 2).sendEvent = "";
            go.name = "bullet" + AmmunitionControl.currAmmoType.AmmoName;
            fireball.GetOrAddComponent<BulletBehavior>().bulletType = AmmunitionControl.currAmmoType;
        }

        //MISC
        public static void Log(string s)
        {
            Modding.Logger.Log("[HOLLOW POINT] " + s);
        }

        public void OnDestroy()
        {
            Log("Destroying Stuff");
            ModHooks.Instance.ObjectPoolSpawnHook -= BooletSize;
            ModHooks.Instance.AttackHook -= Attack_Hook;
            On.NailSlash.StartSlash -= Start_Slash;
            On.HealthManager.Hit -= spellDam;
            Destroy(gameObject.GetComponent<HPControl>());
            Destroy(fireball);
            Destroy(fireballFSM);
            Destroy(fireballControlFSM);
            Destroy(this);
        }

    }
}
