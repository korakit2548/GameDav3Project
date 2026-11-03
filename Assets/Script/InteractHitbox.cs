using System.Collections.Generic;
using UnityEngine;

public class InteractHitbox : MonoBehaviour
{
    private PlayerController player;
    private readonly HashSet<Collider> currentColliders = new HashSet<Collider>();
    private readonly List<Candidate> candidates = new List<Candidate>();
    private readonly HashSet<InteractHighlight> visibleHighlights = new HashSet<InteractHighlight>();
    private readonly HashSet<InteractHighlight> nextHighlights = new HashSet<InteractHighlight>();
    private IInteractable selectedInteractable;
    private IHoldInteractable selectedHold;
    private bool hasStainInRange;

    private struct Candidate
    {
        public IInteractable interactable;
        public IHoldInteractable hold;
        public InteractHighlight highlight;
        public bool canHold;
        public float distance;
    }

    private void Awake() { player = GetComponentInParent<PlayerController>(); }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        currentColliders.Add(other);
        RefreshTool();
    }

    private void OnTriggerExit(Collider other)
    {
        currentColliders.Remove(other);
        RefreshTool();
    }

    private void FixedUpdate()
    {
        bool removed = currentColliders.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy) > 0;
        if (removed || hasStainInRange || IsDestroyed(selectedHold) || IsDestroyed(selectedInteractable)) RefreshTool();
        else if (player != null && player.isActiveAndEnabled && selectedInteractable != null)
            player.SetInteractable(selectedInteractable);
    }

    public void RefreshTool()
    {
        if (player == null) return;
        candidates.Clear();
        nextHighlights.Clear();
        hasStainInRange = false;
        foreach (var collider in currentColliders)
        {
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy) continue;
            var interactable = Resolve<IInteractable>(collider);
            var hold = Resolve<IHoldInteractable>(collider);
            if (interactable == null && hold == null) continue;
            var highlight = Resolve<InteractHighlight>(collider);
            var owner = hold as Component;
            string tag = owner != null ? owner.tag : collider.tag;
            if (tag != "Dirty" && tag != "Trash" && tag != "CursedTrash") tag = collider.tag;
            if (tag == "Dirty")
            {
                hasStainInRange = true;
                var surface = owner != null ? owner.GetComponentInChildren<MeshRenderer>() : collider.GetComponentInChildren<MeshRenderer>();
                var surfaceTransform = surface != null ? surface.transform : collider.transform;
                Vector3 center = surface != null ? surface.bounds.center : collider.bounds.center;
                // The authored stain mesh has four normals pointing along local -Z.
                Vector3 front = surfaceTransform.localToWorldMatrix.inverse.transpose.MultiplyVector(Vector3.back).normalized;
                if (Vector3.Dot(player.transform.position - center, front) <= 0.02f) continue;
                if (Camera.main != null && Vector3.Dot(Camera.main.transform.position - center, front) <= 0.02f) continue;
            }
            bool canHold = hold != null && (tag == "Dirty" && player.GetCurrentTool() == CleaningTool.sponge
                || (tag == "Trash" || tag == "CursedTrash") && player.GetCurrentTool() == CleaningTool.Broom);
            candidates.Add(new Candidate
            {
                interactable = interactable,
                hold = hold,
                highlight = highlight,
                canHold = canHold,
                distance = (collider.ClosestPoint(player.transform.position) - player.transform.position).sqrMagnitude
            });
            if (highlight != null) nextHighlights.Add(highlight);
        }

        IHoldInteractable nextHold = null;
        IInteractable nextInteractable = null;
        float holdDistance = float.PositiveInfinity, interactDistance = float.PositiveInfinity;
        bool keptHold = false, keptInteractable = false;
        foreach (var candidate in candidates)
        {
            if (candidate.canHold && !keptHold)
            {
                if (ReferenceEquals(candidate.hold, selectedHold))
                {
                    nextHold = candidate.hold;
                    keptHold = true;
                }
                else if (candidate.distance < holdDistance)
                {
                    nextHold = candidate.hold;
                    holdDistance = candidate.distance;
                }
            }
            if (candidate.interactable != null && !keptInteractable)
            {
                if (ReferenceEquals(candidate.interactable, selectedInteractable))
                {
                    nextInteractable = candidate.interactable;
                    keptInteractable = true;
                }
                else if (candidate.distance < interactDistance)
                {
                    nextInteractable = candidate.interactable;
                    interactDistance = candidate.distance;
                }
            }
        }
        if (!ReferenceEquals(selectedHold, nextHold) && selectedHold != null) player.RemoveHoldInteractable(selectedHold);
        if (!ReferenceEquals(selectedInteractable, nextInteractable) && selectedInteractable != null) player.RemoveInteractable(selectedInteractable);
        selectedHold = nextHold;
        selectedInteractable = nextInteractable;
        if (selectedHold != null) player.SetHoldInteractable(selectedHold);
        if (selectedInteractable != null) player.SetInteractable(selectedInteractable);

        foreach (var highlight in visibleHighlights)
            if (highlight != null && !nextHighlights.Contains(highlight)) highlight.SetHighlight(false);
        foreach (var highlight in nextHighlights)
        {
            bool showE = false;
            foreach (var candidate in candidates)
            {
                if (candidate.highlight != highlight) continue;
                showE |= selectedHold != null
                    ? candidate.canHold && ReferenceEquals(candidate.hold, selectedHold)
                    : selectedInteractable != null && ReferenceEquals(candidate.interactable, selectedInteractable);
            }
            highlight.SetHighlight(true, showE);
        }
        visibleHighlights.Clear();
        foreach (var highlight in nextHighlights) visibleHighlights.Add(highlight);
    }

    private static T Resolve<T>(Collider collider) where T : class
    {
        return collider.GetComponentInParent<T>() ?? collider.GetComponentInChildren<T>();
    }
    private static bool IsDestroyed(object target) { return target is Object instance && instance == null; }

    private void OnDisable()
    {
        foreach (var highlight in visibleHighlights)
            if (highlight != null) highlight.SetHighlight(false);
        if (player != null)
        {
            if (selectedHold != null) player.RemoveHoldInteractable(selectedHold);
            if (selectedInteractable != null) player.RemoveInteractable(selectedInteractable);
        }
        selectedHold = null;
        selectedInteractable = null;
        currentColliders.Clear();
        visibleHighlights.Clear();
        nextHighlights.Clear();
        candidates.Clear();
        hasStainInRange = false;
    }
}
