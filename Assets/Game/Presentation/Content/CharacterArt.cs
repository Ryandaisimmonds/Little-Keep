using UnityEngine;

namespace LittleKeep.Game
{
    /// One character's animation frames, facing left and right. Made from an art pack by
    /// Little Keep > Apply Art Packs, or by hand. Swapping this asset swaps the character's look.
    [CreateAssetMenu(menuName = "Little Keep/Character Art", fileName = "Art")]
    public sealed class CharacterArt : ScriptableObject
    {
        public Sprite[] idleLeft = new Sprite[0], idleRight = new Sprite[0];
        public Sprite[] moveLeft = new Sprite[0], moveRight = new Sprite[0];
        public Sprite[] attackLeft = new Sprite[0], attackRight = new Sprite[0];
        public float framesPerSecond = 8f;
        [Tooltip("Extra size on top of the game's standard character height (1 = standard)")] public float scale = 1f;
        [Tooltip("Lifts the character on its lane position, in standard heights")] public float footOffset;
        [Header("Measured by Apply Art Packs")]
        [Tooltip("Size of the visible character inside its frame, in world units at scale 1")] public Vector2 visibleSize = new Vector2(1.28f, 1.28f);
        [Tooltip("Centre of the visible character relative to its frame's centre, in world units at scale 1")] public Vector2 visibleCenter;

        /// Height of one frame in world units at scale 1.
        public float FrameHeight => idleRight != null && idleRight.Length > 0 && idleRight[0] != null ? idleRight[0].rect.height / idleRight[0].pixelsPerUnit : 1.28f;

        /// The sprite scale that makes the visible character `height` world units tall.
        public float ScaleFor(float height) => height * scale / Mathf.Max(0.05f, visibleSize.y);

        public Sprite[] Frames(CharacterPose pose, bool faceRight)
        {
            Sprite[] set = pose == CharacterPose.Move ? (faceRight ? moveRight : moveLeft)
                : pose == CharacterPose.Attack ? (faceRight ? attackRight : attackLeft)
                : (faceRight ? idleRight : idleLeft);
            if (set != null && set.Length > 0) return set;
            return faceRight ? idleRight : idleLeft;
        }
    }

    public enum CharacterPose { Idle, Move, Attack }
}
