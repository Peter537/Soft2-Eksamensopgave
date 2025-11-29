Feature: Pick Up Order
    As an agent
    I want to confirm pickup
    So that customer knows en route

    Scenario: Agent confirms pickup for ready order with agent assigned
        Given an order exists with Ready status and agent 42 assigned
        When the agent confirms pickup for the order
        Then the response status code should be 204
        And the order status should be PickedUp

    Scenario: Agent cannot confirm pickup when no agent is assigned
        Given an order exists with Ready status and no agent assigned
        When the agent confirms pickup for the order
        Then the response status code should be 403

    Scenario: OrderPickedUp event is published when pickup is confirmed
        Given an order exists with Ready status and agent 42 assigned
        When the agent confirms pickup for the order
        Then OrderPickedUp kafka event is published with the agent name

    Scenario: Agent cannot confirm pickup for order that is not ready
        Given an order exists with Accepted status
        When the agent confirms pickup for the order
        Then the response status code should be 400

    Scenario: Agent cannot confirm pickup for non-existent order
        When the agent confirms pickup for order 999999
        Then the response status code should be 400
