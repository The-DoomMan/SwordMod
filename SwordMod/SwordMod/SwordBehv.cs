using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace SwordMod
{
    public class SwordBehv : MonoBehaviour
    {
        public float maxlifetime = 300;
        public float spinspeed = 15;
        public float movespeed = 2;
        public float Damage = 1.5f;
        public int MaxBounces = 5;

        [HideInInspector]
        public GameObject owner;
        [HideInInspector]
        public Rigidbody rb;
        public float lifetime;
        public Vector3 movedir;
        public int bounces = 0;
        Transform lerper;
        List<Transform> HitTargets = new List<Transform>();

        public bool parried;
        public bool returnToSender;

        void Awake()
        {
            rb = transform.GetComponent<Rigidbody>();
            movedir = transform.forward;
        }

        void Update()
        {
            transform.gameObject.GetComponentInChildren<MeshRenderer>().gameObject.transform.localEulerAngles = new Vector3(lifetime, transform.rotation.y, transform.rotation.z);
            lifetime += spinspeed;

            if(parried)
            {
                lifetime = 0;
                bounces = 0;
                returnToSender = false;
                parried = false;
            }

            if (lifetime > maxlifetime * 10 || bounces > MaxBounces || returnToSender)
            {
                transform.LookAt(owner.transform);
                rb.velocity = transform.forward * movespeed/1.5f * 10000f * Time.deltaTime;
            }
            else
            {
                rb.velocity = movedir * movespeed * 10000f * Time.deltaTime;
                RaycastHit raycastHit;
                while (bounces < MaxBounces + 1 && Physics.Raycast(transform.position, rb.velocity.normalized, out raycastHit, transform.GetComponent<CapsuleCollider>().radius + 1f))
                {
                    if(raycastHit.transform.gameObject.tag == "Enemy") 
                        return;

                    Breakable breakable;
                    if (raycastHit.transform.TryGetComponent<Breakable>(out breakable))
                    {
                        breakable.Break();
                        return;
                    }

                    movedir = Vector3.Reflect(movedir, raycastHit.normal).normalized;
                    bounces++;
                }
            }
            if (Mathf.Abs(Vector3.Distance(owner.transform.position, transform.position)) <= 3f && (lifetime > maxlifetime * 10 || returnToSender))
            {
                returnToSender = false;
                owner.GetComponent<PunchTest>().caught = true;
                Destroy(transform.gameObject);
            }
        }
        
        void OnTriggerEnter(Collider col)
        {
            if (col.transform != null && !HitTargets.Contains(col.transform))
            {
                if (col.gameObject.tag == "Enemy")
                {
                    HitTargets.Add(col.transform);
                    EnemyIdentifier enemyIdentifier = null;
                    if (col.transform.GetComponent<EnemyIdentifier>())
                    {
                        enemyIdentifier = col.transform.GetComponent<EnemyIdentifier>();
                    }
                    if (enemyIdentifier)
                    {
                        enemyIdentifier.hitter = "Sword";
                        enemyIdentifier.DeliverDamage(col.gameObject, transform.gameObject.transform.forward * 1f * 1000f, transform.position, Damage, false, 0f);
                    }
                }
            }
        }
    }
}
