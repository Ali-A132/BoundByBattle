using UnityEngine;
using UnityEngine.InputSystem;

public class LocalInputReader : MonoBehaviour
{
    public Key moveLeft;
    public Key moveRight;
    public Key moveUp;

    public Key jabKey;
    public Key heavyKey;
    public Key kickKey;
    public Key blockKey;

    PlayerController controller;

    bool blockHeld;
    bool upHeld;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        HandleMovement();
        HandleActions();
        HandleBlock();
    }

    void HandleMovement()
    {
        float x = 0f;
        float y = 0f;

        if (Keyboard.current[moveLeft].isPressed) x = -1f;
        if (Keyboard.current[moveRight].isPressed) x = 1f;
        if (Keyboard.current[moveUp].isPressed) y = 1f;

        controller.OnMove(new Vector2(x, y));
    }

    void HandleActions()
    {
        if (Keyboard.current[jabKey].wasPressedThisFrame)
            controller.OnJab();

        if (Keyboard.current[heavyKey].wasPressedThisFrame)
            controller.OnHeavyPunch();

        if (Keyboard.current[kickKey].wasPressedThisFrame)
            controller.OnKick();

    }

    void HandleBlock()
    {
        bool blockDown = Keyboard.current[blockKey].wasPressedThisFrame;
        bool blockUp = Keyboard.current[blockKey].wasReleasedThisFrame;

        if (blockDown && !blockHeld)
        {
            blockHeld = true;
            controller.OnBlock();
        }

        if (blockUp && blockHeld)
        {
            blockHeld = false;
            controller.OnBlockReleased();
        }
    }
}