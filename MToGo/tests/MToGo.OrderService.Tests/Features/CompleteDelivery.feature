Feature: Complete Delivery
    As an agent
    I want to mark delivered
    So that workflow completes

    Scenario: Agent marks order as delivered for picked up order with agent assigned
        Given an order exists with PickedUp status and agent 42 assigned
        When the agent marks the order as delivered
        Then the response status code should be 204
        And the order status should be Delivered

    Scenario: Agent cannot mark delivery when no agent is assigned
        Given an order exists with PickedUp status and no agent assigned
        When the agent marks the order as delivered
        Then the response status code should be 403

    Scenario: OrderDelivered event is published when delivery is completed
        Given an order exists with PickedUp status and agent 42 assigned
        When the agent marks the order as delivered
        Then OrderDelivered kafka event is published

    Scenario: Agent cannot mark delivery for order that is not picked up
        Given an order exists with Ready status and agent 42 assigned
        When the agent marks the order as delivered
        Then the response status code should be 400

    Scenario: Agent cannot mark delivery for non-existent order
        When the agent marks order 999999 as delivered
        Then the response status code should be 400
