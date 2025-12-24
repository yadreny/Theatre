using UnityEngine;

public abstract class AnimationRandomizer : StateMachineBehaviour
{

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        int id = (int)Mathf.Round(Random.Range(0, len()));
        animator.SetInteger("animationId", id);
    }

    abstract protected int len();
}