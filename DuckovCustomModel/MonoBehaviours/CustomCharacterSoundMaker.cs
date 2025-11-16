using UnityEngine;

namespace DuckovCustomModel.MonoBehaviours
{
    public class CustomCharacterSoundMaker : MonoBehaviour
    {
        public CharacterMainControl? characterMainControl;
        public float customMoveSoundTimer;
        public float moveSoundTimer;
        public CharacterSoundMaker? originalCharacterSoundMaker;
        private float? _customRunSoundFrequency;
        private float? _customWalkSoundFrequency;

        public float WalkSoundFrequency =>
            originalCharacterSoundMaker == null ? 4f : originalCharacterSoundMaker.walkSoundFrequence;

        public float RunSoundFrequency =>
            originalCharacterSoundMaker == null ? 7f : originalCharacterSoundMaker.runSoundFrequence;

        public float WalkSoundDistance =>
            originalCharacterSoundMaker == null ? 0f : originalCharacterSoundMaker.walkSoundDistance;

        public float RunSoundDistance =>
            originalCharacterSoundMaker == null ? 0f : originalCharacterSoundMaker.runSoundDistance;

        public bool SkipSound => characterMainControl == null || characterMainControl.IsInAdsInput ||
                                 !characterMainControl.CharacterItem;

        public float OriginalSoundFrequency => characterMainControl == null || characterMainControl.Running
            ? RunSoundFrequency
            : WalkSoundFrequency;

        public float CustomWalkSoundFrequency
        {
            get => _customWalkSoundFrequency ?? WalkSoundFrequency;
            set => _customWalkSoundFrequency = value;
        }

        public float CustomRunSoundFrequency
        {
            get => _customRunSoundFrequency ?? RunSoundFrequency;
            set => _customRunSoundFrequency = value;
        }

        public float CustomSoundFrequency => characterMainControl == null || characterMainControl.Running
            ? CustomRunSoundFrequency
            : CustomWalkSoundFrequency;

        public bool IsHeavy => characterMainControl != null &&
                               characterMainControl.CharacterItem.TotalWeight /
                               (double)characterMainControl.MaxWeight >= 0.75;

        public CharacterSoundMaker.FootStepTypes FootStepSoundTypes
        {
            get
            {
                if (characterMainControl == null)
                    return CharacterSoundMaker.FootStepTypes.walkLight;
                if (characterMainControl.Running)
                    return IsHeavy
                        ? CharacterSoundMaker.FootStepTypes.runHeavy
                        : CharacterSoundMaker.FootStepTypes.runLight;
                return IsHeavy
                    ? CharacterSoundMaker.FootStepTypes.walkHeavy
                    : CharacterSoundMaker.FootStepTypes.walkLight;
            }
        }

        public void Update()
        {
            if (characterMainControl == null) return;
            if (characterMainControl.movementControl.Velocity.magnitude < 0.5)
            {
                moveSoundTimer = 0.0f;
                customMoveSoundTimer = 0.0f;
                return;
            }

            moveSoundTimer += Time.deltaTime;
            customMoveSoundTimer += Time.deltaTime;

            UpdateAIBrain();
            UpdateFootStepSound();
        }

        private void UpdateAIBrain()
        {
            if (moveSoundTimer < 1.0 / OriginalSoundFrequency) return;
            moveSoundTimer = 0.0f;
            if (SkipSound) return;
            if (characterMainControl == null) return;

            var sound = new AISound
            {
                pos = transform.position,
                fromTeam = characterMainControl.Team,
                soundType = SoundTypes.unknowNoise,
                fromObject = characterMainControl.gameObject,
                fromCharacter = characterMainControl,
            };
            var isHeavy = IsHeavy;
            if (characterMainControl.Running)
            {
                if (RunSoundDistance > 0.0) sound.radius = RunSoundDistance * (isHeavy ? 1.5f : 1f);
            }
            else if (WalkSoundDistance > 0.0)
            {
                sound.radius = WalkSoundDistance * (isHeavy ? 1.5f : 1f);
            }

            AIMainBrain.MakeSound(sound);
        }

        private void UpdateFootStepSound()
        {
            if (customMoveSoundTimer < 1.0 / CustomSoundFrequency) return;
            customMoveSoundTimer = 0.0f;
            if (SkipSound) return;
            if (characterMainControl == null) return;

            if (characterMainControl.Running)
            {
                if (!(RunSoundDistance > 0.0)) return;
                var onFootStepSound = CharacterSoundMaker.OnFootStepSound;
                onFootStepSound?.Invoke(transform.position, FootStepSoundTypes, characterMainControl);
            }
            else if (WalkSoundDistance > 0.0)
            {
                var onFootStepSound = CharacterSoundMaker.OnFootStepSound;
                onFootStepSound?.Invoke(transform.position, FootStepSoundTypes, characterMainControl);
            }
        }
    }
}
