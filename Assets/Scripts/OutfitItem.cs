using UnityEngine;

public class OutfitItem : MonoBehaviour
{
    public enum Slot { Hair, Top, Bottom, Dress }

    public Slot slot;

    [Header("Body parts this item hides")]
    public bool hideTorso;
    public bool hideLegs;
    public bool hideArms;
    public bool hideHead;
    public bool hideFeet;
    public bool hideHands;
}