using NineMood.Core;
using NineMood.Core.Managers;
using NineMood.Data.Enums;
using NineMood.Runner;
using UnityEngine;

namespace NineMood.Presentation
{
    public class PixelRunnerAnimator : MonoBehaviour
    {
        RunnerController runner;SpriteRenderer image;float clock;float floor;
        public void Bind(RunnerController owner,float ground)
        {runner=owner;floor=ground;image=GetComponent<SpriteRenderer>();image.sprite=ConceptArtLibrary.Get("cat-0");}
        void LateUpdate()
        {
            if(runner==null || image==null)return;
            var game=GameManager.Instance;
            bool playing=game!=null && (game.CurrentState==GameState.RunnerPhase || game.CurrentState==GameState.BossPhase);
            if(playing)clock+=Time.deltaTime;
            var balance=DataManager.Instance.BalanceData;
            var state=runner.CurrentState;int frame=0;float y=0,x=0;Color color=Color.white;
            switch(state)
            {
                case RunnerState.Jump:
                    frame=2;y=Mathf.Sin(Mathf.Clamp01(1-runner.ActionTimeRemaining/Mathf.Max(.01f,balance.jumpDuration))*Mathf.PI)*1.2f;break;
                case RunnerState.Slide:frame=3;break;
                case RunnerState.Block:frame=1;color=new Color(1f,1f,.65f);break;
                case RunnerState.Attack:frame=1;x=.25f*Mathf.Sin(Mathf.Clamp01(1-runner.ActionTimeRemaining/Mathf.Max(.01f,balance.attackDuration))*Mathf.PI);break;
                case RunnerState.Stumble:frame=0;color=new Color(1f,.4f,.45f);x=Mathf.Sin(clock*80)*.07f;break;
                case RunnerState.Invincible:frame=1;color=new Color(.6f,1f,1f,.45f+.55f*Mathf.Abs(Mathf.Sin(clock*18)));break;
                default:
                    frame=playing ? ((int)(clock*(runner.BoostTimeRemaining>0 ? 16:10))%2) : 0;
                    y=playing ? Mathf.Abs(Mathf.Sin(clock*15))*.025f : 0;break;
            }
            image.sprite=ConceptArtLibrary.Get("cat-"+frame);image.color=color;
            transform.position=new Vector3(runner.transform.position.x+x,floor+y,runner.transform.position.z-.2f);
        }
    }
}
