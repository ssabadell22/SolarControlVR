using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

// Adapted from https://www.youtube.com/watch?v=qbCEHCVx-Dc
public class SpinningWheel : XRBaseInteractable
{
    [Header("Spinning Wheel")]
    [SerializeField] private Transform _rotationPivot;
    public UnityEvent<float> OnWheelRotated;

    private float _currentAngle = 0f;

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        _currentAngle = FindWheelAngle();
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        _currentAngle = FindWheelAngle();
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);
        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        {
            if (isSelected)
                RotateWheel();
        }
    }

    private void RotateWheel()
    {
        // Convert direction to an angle, then rotation
        float totalAngle = FindWheelAngle();
        // Apply difference
        float angleDifference = _currentAngle - totalAngle;
        _rotationPivot.Rotate(transform.forward, -angleDifference);
        // Store it for the next rotation delta
        _currentAngle = totalAngle;
        OnWheelRotated?.Invoke(angleDifference);
    }

    private float FindWheelAngle()
    {
        float totalAngle = 0f;
        // Combine directions of current interactors (in case there is more than one)
        foreach (var interactor in interactorsSelecting)
        {
            Vector2 direction = FindLocalPoint(interactor.transform.position);
            totalAngle += ConvertToAngle(direction) * FindRotationSensitivity();
        }
        return totalAngle;
    }

    private Vector2 FindLocalPoint(Vector3 position)
    {
        // convert hand positions to local to make it easier to find the angle
        return transform.InverseTransformPoint(position).normalized;
    }

    private float ConvertToAngle(Vector2 direction)
    {
        // use consistent up direction to find angle
        return Vector2.SignedAngle(transform.up, direction);
    }

    private float FindRotationSensitivity()
    {
        // use smaller sensitivity with two hands
        return 1f / interactorsSelecting.Count;
    }
}
