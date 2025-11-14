using UnityEngine;

namespace DuckovCustomModel.MonoBehaviours
{
    public class CustomCharacterSoundMaker : CharacterSoundMaker
    {
        public float customMoveSoundTimer;
        public float customWalkSoundFrequency = 4;
        public float customRunSoundFrequency = 7;

        public bool SkipSound => characterMainControl.IsInAdsInput || !characterMainControl.CharacterItem;

        public float OriginalSoundFrequency => characterMainControl.Running ? runSoundFrequence : walkSoundFrequence;

        public float CustomSoundFrequency =>
            characterMainControl.Running ? customRunSoundFrequency : customWalkSoundFrequency;

        public bool IsHeavy =>
            (double)characterMainControl.CharacterItem.TotalWeight / characterMainControl.MaxWeight >= 0.75f;

        public float SoundRadius
        {
            get
            {
                var isHeavy = IsHeavy;
                if (characterMainControl.Running)
                {
                    if (runSoundDistance <= 0f) return 0f;
                    return runSoundDistance * (isHeavy ? 1.5f : 1f);
                }

                if (walkSoundDistance <= 0f) return 0f;
                return walkSoundDistance * (isHeavy ? 1.5f : 1f);
            }
        }

        public FootStepTypes FootStepSoundTypes
        {
            get
            {
                if (characterMainControl.Running) return IsHeavy ? FootStepTypes.runHeavy : FootStepTypes.runLight;
                return IsHeavy ? FootStepTypes.walkHeavy : FootStepTypes.walkLight;
            }
        }

        public new void Update()
        {
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
            if (!(moveSoundTimer >= 1.0 / OriginalSoundFrequency)) return;
            moveSoundTimer = 0.0f;
            if (SkipSound) return;
            var sound = new AISound
            {
                pos = transform.position,
                fromTeam = characterMainControl.Team,
                soundType = SoundTypes.unknowNoise,
                fromObject = characterMainControl.gameObject,
                fromCharacter = characterMainControl,
                radius = SoundRadius,
            };
            AIMainBrain.MakeSound(sound);
        }

        private void UpdateFootStepSound()
        {
            if (!(customMoveSoundTimer >= 1.0 / CustomSoundFrequency)) return;
            customMoveSoundTimer = 0.0f;
            if (SkipSound) return;
            var onFootStepSound = OnFootStepSound;
            onFootStepSound?.Invoke(transform.position, FootStepSoundTypes, characterMainControl);
        }
    }
}