using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Animations;
using ULTRAKILL;


namespace SwordMod
{
    public class PunchTest : MonoBehaviour
    {
        public Animator animator;
        float holdtime = 0f;

        public float CD = 5f;
        public float range = 5f;
        public float Damage = 1.25f;
        public float force = 5f;
        
        
        public bool caught;
        bool thrown;
        bool holding;
        bool thirdhit;
        public GameObject Sword;
        public GameObject mtarget;
        public GameObject fc;
        public Transform throwPoint;
        public Transform HoldPoint;
        public CameraController cc;
        public int hitcount;
        public float timesincelastpunch;


        LayerMask EnemyTrigger = 12;
        LayerMask Projectiles = 14;
        LayerMask Items = 22;

        public ParticleSystemRenderer[] particleSystems;

        public StyleHUD shud;
        public AudioSource aud;
        public NewMovement nmov;
        public ItemIdentifier heldItem;
        public Transform parryobject;
        GameObject ThrownSword;
        public bool called = false;

        void Update()
        {
            if(thrown && ThrownSword)
            {
                fc.transform.LookAt(ThrownSword.transform);
            }
            else
            {
                Quaternion.RotateTowards(base.transform.parent.localRotation, Quaternion.identity, (Quaternion.Angle(base.transform.parent.localRotation, Quaternion.identity) * 5f + 5f) * Time.deltaTime * 5f);
            }
            if (CD > 0)
            { CD -= 15f * Time.deltaTime;}
            if(CD < 0)
            {
                CD = 0;
            }
            if(thirdhit && CD == 0)
            {
                hitcount = 0;
                thirdhit = false;
            }
            if(hitcount > 0)
            {
                timesincelastpunch += Time.deltaTime;
            }
            if (timesincelastpunch > 90f * Time.deltaTime)
            {
                hitcount = 0;
            }                
            if (MonoSingleton<InputManager>.Instance.InputSource.Punch.IsPressed && CD <= 0  && !thrown && hitcount <= 3)
            {
                hitcount++; timesincelastpunch = 0f;
                if (hitcount == 3)
                {
                    thirdhit = true;
                    Debug.Log("thirdhit");
                }
                if(hitcount == 2)
                { animator.Play("Base Layer.SWSwing2", 0); }    
                else {animator.CrossFade("Base Layer.SWswing", 0); }
                //enemy checker
                Collider[] array = Physics.OverlapSphere(throwPoint.position, range);
                if (array.Length != 0)
                {
                    foreach (Collider col in array)
                    {
                        if (col.gameObject.tag == "Enemy")
                        {
                            //check what type of thing we can actually hit to avoid null 
                            if (col.gameObject.GetComponentInChildren<EnemySimplifier>() && !mtarget)
                            { 
                                mtarget = col.gameObject.GetComponentInChildren<EnemySimplifier>().gameObject;

                            }
                            else if (col.gameObject.GetComponentInChildren<EnemyIdentifierIdentifier>() && !mtarget)
                            {
                                mtarget = col.gameObject.GetComponentInChildren<EnemyIdentifierIdentifier>().gameObject;
                            }
                            else if (!mtarget)
                            { 
                                mtarget = col.gameObject.GetComponentInChildren<EnemyIdentifier>().gameObject;
                            }
                            
                            if(mtarget)
                            { PunchSuccess(mtarget.transform.position, col.gameObject.transform, false); mtarget = null; }
                        }
                    }
                }
                
                //item checker
                Collider[] array3 = Physics.OverlapSphere(throwPoint.position, range, 1 << Items);
                if (array3.Length != 0)
                {
                    foreach (Collider collider in array3)
                    {
                        if (!holding)
                        {
                            HoldPoint = transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(0).transform.GetChild(6).transform;
                            ItemIdentifier component = collider.gameObject.transform.GetComponent<ItemIdentifier>();
                            ItemPlaceZone[] components = collider.gameObject.transform.GetComponents<ItemPlaceZone>();
                            if (holding && components != null && components.Length != 0)
                            {
                                PlaceHeldObject(components, collider.gameObject.transform);
                            }
                            else if (!this.holding && component != null)
                            {
                                ForceHold(component);
                            }
                            animator.SetBool("Holding", true);
                            holding = true;
                        }
                        else {
                             animator.SetBool("Holding", false);
                             holding = false;
                        }
                    }
                }

                thrown = false;
                CD = 4f * hitcount;
            }
            else if (MonoSingleton<InputManager>.Instance.InputSource.Punch.IsPressed && CD <= 0 && thrown && MonoSingleton<InputManager>.Instance.InputSource.Punch.WasPerformedThisFrame)
            {
                //parry checker
                Collider[] array4 = Physics.OverlapSphere(throwPoint.position, range * 1.5f);
                if (array4.Length != 0)
                {
                    foreach (Collider collider in array4)
                    {
                        if (collider.GetComponent<SwordBehv>())
                        {
                            parryobject = collider.transform;
                        }
                    }
                }
                if(parryobject != null)
                {
                    aud.pitch = Random.Range(0.7f, 0.8f);
                    MonoSingleton<TimeController>.Instance.ParryFlash();
                    shud.AddPoints(100, "<color=lime>PING PONG</color>");
                    animator.SetTrigger("ParrySuccess");
                    parryobject.GetComponent<SwordBehv>().movedir = GameObject.FindWithTag("MainCamera").transform.forward;
                    parryobject.GetComponent<SwordBehv>().parried = true;
                    parryobject = null;
                }
                else
                {
                    //the fail animation looked shit okay ?, i hate the unity animator
                    //ps the success animation doesnt look any better, so fix that idiot!!!!!!
                    animator.SetTrigger("ParrySuccess");
                }
                CD = 4f;
            }
            if (MonoSingleton<InputManager>.Instance.InputSource.Punch.IsPressed)
            { holdtime += 20f * Time.deltaTime; }
            else if (!MonoSingleton<InputManager>.Instance.InputSource.Punch.IsPressed)
            {
                holdtime = 0;
            }
            if (holdtime > 15f && thrown && ThrownSword && !called)
            {
                called = true;
                ThrownSword.GetComponent<SwordBehv>().returnToSender = true;
                animator.SetTrigger("PullSword");
            }
            else if (holdtime > 5f && !thrown && !called)
            {
                thrown = true;
                for(int i = 0; i < particleSystems.Length; i++)
                {
                    particleSystems[i].gameObject.SetActive(false);
                }
                animator.Play("Base Layer.SWThrow", 0, 1f);
                GameObject SW = Instantiate(Sword, throwPoint);
                SW.transform.eulerAngles = GameObject.FindGameObjectWithTag("MainCamera").transform.eulerAngles;
                SW.AddComponent<SwordBehv>();
                SW.GetComponent<SwordBehv>().owner = transform.gameObject;
                ThrownSword = SW;
                SW.gameObject.transform.SetParent(null);
                Debug.Log("throw");
            }
            if (caught)
            {
                animator.Play("Base Layer.SWCatch", 0, 1f);
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    particleSystems[i].gameObject.SetActive(true);
                }
                called = false;
                thrown = false;
                caught = false;
            }
        }


        public void ResetHeldState()
        {
            holding = false;
        }

        public void ForceHold(ItemIdentifier itid)
        {
            this.holding = true;
            ItemPlaceZone[] componentsInParent = itid.GetComponentsInParent<ItemPlaceZone>();
            itid.ipz = null;
            this.heldItem = itid;
            itid.transform.SetParent(HoldPoint);
            GameObject.FindGameObjectWithTag("MainCamera").GetComponentInChildren<FistControl>().heldObject = itid;
            itid.pickedUp = true;
            ResetHeldItemPosition();
            Transform[] componentsInChildren = this.heldItem.GetComponentsInChildren<Transform>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].gameObject.layer = 13;
            }
            Rigidbody componentInChildren = this.heldItem.GetComponentInChildren<Rigidbody>();
            if (componentInChildren != null)
            {
                componentInChildren.isKinematic = true;
            }
            Collider componentInChildren2 = this.heldItem.GetComponentInChildren<Collider>();
            if (componentInChildren2 != null)
            {
                componentInChildren2.enabled = false;
            }
            Object.Instantiate<GameObject>(itid.pickUpSound);
            this.heldItem.SendMessage("PickUp", SendMessageOptions.DontRequireReceiver);
            if (componentsInParent != null && componentsInParent.Length != 0)
            {
                ItemPlaceZone[] array = componentsInParent;
                for (int i = 0; i < array.Length; i++)
                {
                    array[i].CheckItem(false);
                }
            }
        }

        public void ResetHeldItemPosition()
        {
            if (this.heldItem.reverseTransformSettings)
            {
                this.heldItem.transform.localPosition = this.heldItem.putDownPosition;
                this.heldItem.transform.localScale = this.heldItem.putDownScale;
                this.heldItem.transform.localRotation = Quaternion.Euler(this.heldItem.putDownRotation);
            }
            else
            {
                this.heldItem.transform.localPosition = Vector3.zero;
                this.heldItem.transform.localScale = Vector3.one;
                this.heldItem.transform.localRotation = Quaternion.identity;
            }
            Transform[] componentsInChildren = this.heldItem.GetComponentsInChildren<Transform>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].gameObject.layer = 13;
            }
        }

        public void PlaceHeldObject(ItemPlaceZone[] placeZones, Transform target)
        {
            if (!this.heldItem)
            {
                this.ResetHeldState();
                return;
            }
            holding = false;
            heldItem.transform.SetParent(target);
            heldItem.pickedUp = false;
            if (this.heldItem.reverseTransformSettings)
            {
                this.heldItem.transform.localPosition = Vector3.zero;
                this.heldItem.transform.localScale = Vector3.one;
                this.heldItem.transform.localRotation = Quaternion.identity;
            }
            else
            {
                this.heldItem.transform.localPosition = this.heldItem.putDownPosition;
                this.heldItem.transform.localScale = this.heldItem.putDownScale;
                this.heldItem.transform.localRotation = Quaternion.Euler(this.heldItem.putDownRotation);
            }
            Transform[] componentsInChildren = this.heldItem.GetComponentsInChildren<Transform>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                componentsInChildren[i].gameObject.layer = 22;
            }
            Rigidbody componentInChildren = this.heldItem.GetComponentInChildren<Rigidbody>();
            if (componentInChildren != null)
            {
                componentInChildren.isKinematic = false;
            }
            Collider componentInChildren2 = this.heldItem.GetComponentInChildren<Collider>();
            if (componentInChildren2 != null)
            {
                componentInChildren2.enabled = true;
            }
            heldItem.SendMessage("PutDown", SendMessageOptions.DontRequireReceiver);
            Object.Instantiate<GameObject>(this.heldItem.pickUpSound);
            heldItem = null;
            GameObject.FindGameObjectWithTag("MainCamera").GetComponentInChildren<FistControl>().heldObject = null;
            for (int i = 0; i < placeZones.Length; i++)
            {
                placeZones[i].CheckItem(false);
            }
            this.ResetHeldState();
        }

        private void PunchSuccess(Vector3 point, Transform target, bool lockon)
        {
            if(lockon)
            base.transform.parent.LookAt(point);
            if (Quaternion.Angle(base.transform.parent.localRotation, Quaternion.identity) > 45f)
            {
                Quaternion localRotation = base.transform.parent.localRotation;
                float num = localRotation.eulerAngles.x;
                float num2 = localRotation.eulerAngles.y;
                float num3 = localRotation.eulerAngles.z;
                if (num > 180f)
                {
                    num -= 360f;
                }
                if (num2 > 180f)
                {
                    num2 -= 360f;
                }
                if (num3 > 180f)
                {
                    num3 -= 360f;
                }
                localRotation.eulerAngles = new Vector3(Mathf.Clamp(num, -45f, 45f), Mathf.Clamp(num2, -45f, 45f), Mathf.Clamp(num3, -45f, 45f));
                base.transform.parent.localRotation = localRotation;
            }
            if (target.gameObject.tag == "Enemy" || target.gameObject.tag == "Head" || target.gameObject.tag == "Body" || target.gameObject.tag == "Limb" || target.gameObject.tag == "EndLimb")
            {
                MonoSingleton<TimeController>.Instance.HitStop(0.1f);
                this.cc.CameraShake(0.5f * 1);
                EnemyIdentifier enemyIdentifier = null;
                if(target.GetComponent<EnemyIdentifier>())
                {
                    enemyIdentifier = target.GetComponent<EnemyIdentifier>();
                }
                if (enemyIdentifier)
                {
                    if (enemyIdentifier.drillers.Count > 0)
                    {
                        MonoSingleton<TimeController>.Instance.ParryFlash();
                        enemyIdentifier.drillers[enemyIdentifier.drillers.Count - 1].transform.forward = this.cc.transform.forward;
                        enemyIdentifier.drillers[enemyIdentifier.drillers.Count - 1].transform.position = this.cc.GetDefaultPos();
                        enemyIdentifier.drillers[enemyIdentifier.drillers.Count - 1].Punched();
                    }
                    enemyIdentifier.hitter = "heavypunch";
                    enemyIdentifier.DeliverDamage(target.gameObject, cc.gameObject.transform.forward * this.force * (1000f + 100f * (2f * hitcount)), point, Damage + (1.2f * hitcount), false, 0f);
                }
                /*if (this.holding)
                {
                    this.heldItem.SendMessage("HitWith", target.gameObject, SendMessageOptions.DontRequireReceiver);
                    return;
                }*/
            }
           else if (target.gameObject.tag == "Coin")
            {
                Coin component = target.GetComponent<Coin>();
                if (component && component.doubled)
                {
                    Coin component2 = target.GetComponent<Coin>();
                    if (component2 == null)
                    {
                        return;
                    }
                    component2.DelayedPunchflection();
                }
            }
        }

        public void Parry(bool hook = false)
        {
            aud.pitch = Random.Range(0.7f, 0.8f);
            MonoSingleton<TimeController>.Instance.ParryFlash();
            nmov.GetHealth(999, false);
            shud.AddPoints(100, "<color=lime>PARRY</color>");
        }

        private void ParryProjectile(Projectile proj)
        {
            proj.hittingPlayer = false;
            proj.friendly = true;
            proj.speed *= 2f;
            proj.homingType = HomingType.None;
            //proj.explosionEffect = this.parriedProjectileHitObject;
            proj.precheckForCollisions = true;
            Rigidbody component = proj.GetComponent<Rigidbody>();
            if (proj.playerBullet)
            {
                //this.alreadyBoostedProjectile = true;
                proj.boosted = true;
                proj.GetComponent<SphereCollider>().radius *= 4f;
                proj.damage = 0f;
                if (component)
                {
                    component.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                }
            }
            if (component)
            {
                component.constraints = RigidbodyConstraints.FreezeRotation;
            }
            if (!proj.playerBullet)
            {
                Parry(false);
            }
            else
            {
                MonoSingleton<TimeController>.Instance.ParryFlash();
            }
            if (proj.explosive)
            {
                proj.explosive = false;
            }
            Rigidbody component2 = proj.GetComponent<Rigidbody>();
            if (component2 && component2.useGravity)
            {
                component2.useGravity = false;
            }
            Vector3 parryLookTarget = Punch.GetParryLookTarget();
            proj.transform.LookAt(parryLookTarget);
            if (proj.speed == 0f)
            {
                component2.velocity = (parryLookTarget - base.transform.position).normalized * 250f;
            }
            else if (proj.speed < 100f)
            {
                proj.speed = 100f;
            }
            if (proj.spreaded)
            {
                ProjectileSpread componentInParent = proj.GetComponentInParent<ProjectileSpread>();
                if (componentInParent != null)
                {
                    componentInParent.ParriedProjectile();
                }
            }
            proj.transform.SetParent(null, true);
        }
    }
}
