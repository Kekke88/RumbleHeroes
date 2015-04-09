﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerController : MonoBehaviour
{

    public float speed;
    public int currentHP = 100;

    private Rigidbody2D playerRigidBody;
    private PhotonView punView;
    private Animator animator;
    private bool isGrounded;
    public GameObject projectile;
    public GameObject deathParticles;
    public GameObject hitParticles;

    private float someScale;

    private float lastFired;

    void Start()
    {
        playerRigidBody = GetComponent<Rigidbody2D>();
        punView = GetComponent<PhotonView>();
        animator = GetComponent<Animator>();

        someScale = transform.localScale.x; // assuming this is facing right
    }

    void Update()
    {
        if (punView.isMine)
        {
            // ROFL //
            if(currentHP <= 0)
            {
                object[] paramsForRPC = new object[1];
                paramsForRPC[0] = transform.position;

                punView.RPC("PlayDeathAnimation", PhotonTargets.All, paramsForRPC);

                PhotonNetwork.Destroy(gameObject);
                Destroy(gameObject);

                GameObject tmpPlayer = PhotonNetwork.Instantiate("ThePlayer", new Vector3(0f, 3.5f, 0f), Quaternion.identity, 0);
                GameObject.Find("Main Camera").GetComponent<SmoothCameraFollow>().target = tmpPlayer.transform;
            }
            // ENDROFL //
            InputMovement();
        }
    }

    void InputMovement()
    {
        Vector2 curVel = playerRigidBody.velocity;
        curVel.x = (float)(Input.GetAxis("Horizontal") * speed);
        playerRigidBody.velocity = curVel;

        if(playerRigidBody.velocity.x > 0)
        {
            animator.SetBool("Running", true);
            transform.localScale = new Vector2(someScale, transform.localScale.y);
        }
        else if(playerRigidBody.velocity.x < 0)
        {
            animator.SetBool("Running", true);
            transform.localScale = new Vector2(-someScale, transform.localScale.y);
        }
        else
        {
            animator.SetBool("Running", false);
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            playerRigidBody.AddForce(new Vector2(0, 25), ForceMode2D.Impulse);
            isGrounded = false;
            animator.SetBool("Jumping", true);
        }

        if(Input.GetKeyDown(KeyCode.Mouse1))
        {
            if (Time.time - lastFired > 1)
            {
                //Quaternion projectileRotation = Quaternion.FromToRotation(transform.position,
                //Input.mousePosition);

                //Angle the projectile towards the mouse
                Vector3 mousePos = Input.mousePosition;
                mousePos.z = 10;
                Vector3 playerPos = Camera.main.WorldToScreenPoint(transform.position);
                mousePos.x = mousePos.x - playerPos.x;
                mousePos.y = mousePos.y - playerPos.y;
                float angle = Mathf.Atan2(mousePos.y, mousePos.x) * Mathf.Rad2Deg;

                Quaternion projectileRotation = Quaternion.Euler(new Vector3(0, 0, angle));

                //PUN RPC Call
                object[] paramsForRPC = new object[3];

                paramsForRPC[0] = transform.position;
                paramsForRPC[1] = projectileRotation;
                paramsForRPC[2] = punView.ownerId;


                punView.RPC("FireProjectile", PhotonTargets.All, paramsForRPC);
                lastFired = Time.time;
            }
        }
    }

    [RPC] void PlayDeathAnimation(Vector3 pos)
    {
        Instantiate(deathParticles, pos, Quaternion.identity);
    }

    [RPC] void ProjectileHit(int damage, int playerHitId, Vector3 positionOfImpact)
    {
        Instantiate(hitParticles, positionOfImpact, Quaternion.identity);

        //If I am the player who got hit
        if (punView.ownerId == playerHitId)
            this.currentHP -= damage;
    }

    [RPC] void FireProjectile(Vector3 pos, Quaternion rot, int ownerId)
    {
        Debug.Log("Firing Projectile");
        GameObject tmpProjectile = Instantiate(projectile, pos, rot) as GameObject;
        tmpProjectile.GetComponent<FireBolt>().ownerId = ownerId;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.tag == "Ground")
        {
            isGrounded = true;
            animator.SetBool("Jumping", false);
        }
    }
}
