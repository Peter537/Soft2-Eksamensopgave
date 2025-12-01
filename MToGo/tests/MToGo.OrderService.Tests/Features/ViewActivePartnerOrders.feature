Feature: View Active Partner Orders
    As a partner
    I want to view my active orders
    So that I can manage orders that are in progress

    Scenario: Partner views active orders when they have active orders
        Given a partner with ID 1 has active and completed orders
        When the partner requests their active orders
        Then the response status code should be 200
        And the response contains only orders with active status for partner

    Scenario: Partner views active orders when they have no active orders
        Given a partner with ID 1 has only completed orders
        When the partner requests their active orders
        Then the response status code should be 200
        And the response contains an empty list

    Scenario: Partner views active orders when they have no orders at all
        Given a partner with ID 1 has no orders in the system
        When the partner requests their active orders
        Then the response status code should be 200
        And the response contains an empty list

    Scenario: Partner active orders excludes delivered orders
        Given a partner with ID 1 has an order with status Delivered
        When the partner requests their active orders
        Then the response status code should be 200
        And the response contains an empty list

    Scenario: Partner active orders excludes rejected orders
        Given a partner with ID 1 has an order with status Rejected
        When the partner requests their active orders
        Then the response status code should be 200
        And the response contains an empty list
