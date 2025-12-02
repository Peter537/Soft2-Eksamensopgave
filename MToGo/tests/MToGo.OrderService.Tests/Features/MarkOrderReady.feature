Feature: Mark Order Ready
    As a partner
    I want to mark order prepared
    So that agent can pick up

    Scenario: Successfully mark accepted order as ready
        Given an order exists with Accepted status
        When the partner marks the order as ready
        Then the order status should be Ready
        And the response status code should be 204

    Scenario: OrderReady kafka event published when marking ready
        Given an order exists with Accepted status
        When the partner marks the order as ready
        Then OrderReady kafka event is published

    Scenario: Order can be marked ready without agent assigned
        Given an order exists with Accepted status and no agent assigned
        When the partner marks the order as ready
        Then the order status should be Ready
        And the response status code should be 204

    Scenario: Cannot mark order ready that is not in Accepted status
        Given an order exists with Placed status
        When the partner marks the order as ready
        Then the response status code should be 400
