Feature: View Active Customer Orders
    As a customer
    I want to view my active orders
    So that I can track orders that are in progress

    Scenario: Customer views active orders when they have active orders
        Given a customer with ID 1 has active and completed orders
        When the customer requests their active orders
        Then the response status code should be 200
        And the response contains only orders with active status for customer

    Scenario: Customer views active orders when they have no active orders
        Given a customer with ID 1 has only completed orders
        When the customer requests their active orders
        Then the response status code should be 200
        And the response contains an empty list

    Scenario: Customer views active orders when they have no orders at all
        Given a customer with ID 1 has no orders
        When the customer requests their active orders
        Then the response status code should be 200
        And the response contains an empty list

    Scenario: Active orders excludes delivered orders
        Given a customer with ID 1 has an order with status Delivered
        When the customer requests their active orders
        Then the response status code should be 200
        And the response contains an empty list

    Scenario: Active orders excludes rejected orders
        Given a customer with ID 1 has an order with status Rejected
        When the customer requests their active orders
        Then the response status code should be 200
        And the response contains an empty list
