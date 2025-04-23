using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TokenScript : MonoBehaviour
{
    public Animator animator;
    private AudioSource audioSource;

    void Start()
    {
        animator = GetComponent<Animator>();
        audioSource = FindObjectOfType<GameManager>().audioSource;
    }

    // Called by animation events at the end of flip animations
    public void OnFlipComplete()
    {
        // This function is called by animation events when a flip animation completes
        // The placement sound is played in the GameManager's coroutine
    }

    // This function can be used to manually set the token's state
    public void SetState(string stateName)
    {
        animator.Play(stateName);
    }
}