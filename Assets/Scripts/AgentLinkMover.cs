using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum OffMeshLinkMoveMethod { NormalSpeed, Parabola, Curve} 

public class AgentLinkMover : MonoBehaviour
{
    public OffMeshLinkMoveMethod m_Method = OffMeshLinkMoveMethod.Parabola;
    public AnimationCurve m_Curve = new AnimationCurve();
    private NavMeshAgent agent;
    private Animator animator;
    private EnemyAI enemyAI;

    IEnumerator Start()
    {
        enemyAI = GetComponent<EnemyAI>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        agent.autoTraverseOffMeshLink = false;

        while (true)
        {
            if (enemyAI.isDead)
            {
                animator.SetBool("inJumpingState", false);
                agent.enabled = false;  // Disable the agent to prevent further off-mesh link completion
                yield return null;
                continue;
            }

            if (agent.isOnOffMeshLink)
            {
                if (m_Method == OffMeshLinkMoveMethod.Parabola)
                    yield return StartCoroutine(Parabola(agent, 1.0f, 0.7f));
                else if (m_Method == OffMeshLinkMoveMethod.Curve)
                    yield return StartCoroutine(Curve(agent, 0.5f));

                // Ensure the agent is still valid and on the off-mesh link before completing it
                if (agent.isActiveAndEnabled && agent.isOnOffMeshLink)
                {
                    agent.CompleteOffMeshLink();
                }
            }
            yield return null;
        }
    }


    IEnumerator Parabola(NavMeshAgent agent, float height, float duration)
    {

        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 startPos = agent.transform.position;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
        float normalizedTime = 0.0f;
        while (normalizedTime < 1.0f)
        {

            animator.SetBool("inJumpingState", true);

            float yOffset = height * 4.0f * (normalizedTime - normalizedTime * normalizedTime);
            agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * Vector3.up;
            normalizedTime += Time.deltaTime / duration;
            yield return null;
        }
    }

    IEnumerator Curve(NavMeshAgent agent, float duration)
    {

        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 startPos = agent.transform.position;
        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
        float normalizedTime = 0.0f;
        while (normalizedTime < 1.0f)
        {
            float yOffset = m_Curve.Evaluate(normalizedTime);
            agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * Vector3.up;
            normalizedTime += Time.deltaTime / duration;
            yield return null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!agent.isOnOffMeshLink)
        {
            animator.SetBool("inJumpingState", false);
        }
    }
}
