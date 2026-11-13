using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace NineMood.Core.Fx
{
    public class Fx : FxCore
    {
        #region Sources

        [Serializable]
        public class SpriteRendererSource : BaseSource
        {
            public SpriteRenderer spriteRenderer;
        }

        [Serializable]
        public class TextMeshProSource : BaseSource
        {
            public TextMeshPro textMeshPro;
        }

        [SerializeField, HideInInspectorCustom] protected List<ParticleSystemSource> particleSystems;
        [SerializeField, HideInInspectorCustom] protected List<SpriteRendererSource> spriteRenderers;
        [SerializeField, HideInInspectorCustom] protected List<TextMeshProSource> tmpList;

        [SerializeField, HideInInspectorCustom] private bool foldoutSprites;
        [SerializeField, HideInInspectorCustom] private bool foldoutTMPList;

        #endregion

        #region Animations


        #endregion

        [HideInInspectorCustom] public List<ParticleSystemSource> ParticleSystems => particleSystems;

        [HideInInspectorCustom, SerializeField] protected Vector3 offset;

        private GameObject target;
        private Renderer targetRenderer;

        public GameObject Target
        {
            get => target;
            set
            {
                target = value;
                // targetRenderer = value.MainRenderer;
            }
        }

        public Action OnCompleteAction
        {
            get => onCompleteAction;
            set => onCompleteAction = value;
        }

        public override float ParticleDuration
        {
            get
            {
                if (particleDuration < 0)
                {

                }

                return particleDuration;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            targetRenderer = null;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            Update();

            void Update()
            {
                if (targetRenderer == null)
                    return;

                transform.position = target.transform.position + offset;

                var order = targetRenderer.sortingOrder;
                foreach (var source in particleSystems)
                {
                    source.particleSystemsRenderer.sortingOrder = source.relativeOrder + order;
                }
                foreach (var source in spriteRenderers)
                {
                    source.spriteRenderer.sortingOrder = source.relativeOrder + order;
                }
                foreach (var source in tmpList)
                {
                    source.textMeshPro.sortingOrder = source.relativeOrder + order;
                }
            }
        }

        protected override void InitComponent()
        {
            base.InitComponent();

            foreach (var particle in particleSystems)
            {
                particle.particleSystemsRenderer = particle.particleSystem.GetComponent<ParticleSystemRenderer>();
            }
        }
    }
}