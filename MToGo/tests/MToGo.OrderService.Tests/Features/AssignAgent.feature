Feature: Assign Agent to Order
    As an agent
    I want to accept order assignment
    So that I secure work

    Scenario: Successfully assign agent to Ready order
        Given an order exists with Ready status and no agent assigned
        When the agent accepts the delivery offer with agentId 42
        Then the order should have agent 42 assigned
        And the response status code should be 204

    Scenario: AgentAssigned kafka event published on assignment
        Given an order exists with Ready status and no agent assigned
        When the agent accepts the delivery offer with agentId 42
        Then AgentAssigned kafka event is published

    Scenario: Cannot assign agent to order that already has an agent (race condition)
        Given an order exists with Ready status and agent 10 assigned
        When the agent accepts the delivery offer with agentId 42
        Then the response status code should be 409
        And the order should still have agent 10 assigned

    Scenario: Cannot assign agent to order that is not in Ready or Accepted status
        Given an order exists with Placed status and no agent assigned
        When the agent accepts the delivery offer with agentId 42
        Then the response status code should be 400

    Scenario: Concurrent agent assignment handles race condition gracefully
        Given an order exists with Ready status and no agent assigned
        When two agents try to accept the delivery offer concurrently with agentIds 42 and 43
        Then exactly one agent should be assigned to the order
        And one response should be 204 and the other should be 409
