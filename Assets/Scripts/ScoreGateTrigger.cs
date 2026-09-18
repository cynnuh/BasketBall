using UnityEngine;

public class ScoreGateTrigger : MonoBehaviour
{
    public bool isTopTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball")) return;

        BallThrower ball = other.GetComponent<BallThrower>();
        if (ball == null) return;

        if (isTopTrigger) ball.MarkPassedTop();
        else ball.TryScoreFromBottom();
    }
}
