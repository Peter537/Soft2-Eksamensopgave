Feature: Place Food Order
    As a customer
    I want to place order with items and payment
    So that food is delivered

    Scenario: Successful order placement with valid payment
        Given items in cart
        And valid payment
        When submitting the order
        Then order is created in Placed status with an ID

    Scenario: OrderCreated event published on success
        Given successful order creation
        When order is created
        Then OrderCreated kafka event is published

    Scenario: Service fee is 6% for orders of 100 DKK or less
        Given an order with total value of 100 DKK
        When the order is placed
        Then the service fee should be 6 DKK

    Scenario: Service fee is 3% for orders of 1000 DKK or more
        Given an order with total value of 1000 DKK
        When the order is placed
        Then the service fee should be 30 DKK

    Scenario: Service fee scales linearly between 100 and 1000 DKK
        Given an order with total value of 550 DKK
        When the order is placed
        Then the service fee should be 24.75 DKK
