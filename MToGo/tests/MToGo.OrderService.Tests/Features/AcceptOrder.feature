Feature: Accept Order
    As a partner
    I want to accept placed order
    So that preparation starts

    Scenario: Successfully accept order with Placed status
        Given an order exists with Placed status
        When the partner accepts the order
        Then the order status should be Accepted
        And the response status code should be 204

    Scenario: OrderAccepted kafka event published on acceptance
        Given an order exists with Placed status
        When the partner accepts the order
        Then OrderAccepted kafka event is published

    Scenario: Cannot accept order that is not in Placed status
        Given an order exists with Accepted status
        When the partner accepts the order
        Then the response status code should be 400
