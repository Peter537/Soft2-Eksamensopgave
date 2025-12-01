Feature: View Active Agent Orders
    As an agent
    I want to view my active orders
    So that I can manage orders that I am currently handling

    Scenario: Agent views active orders when they have active orders
        Given an agent with ID 1 has active and completed orders
        When the agent requests their active orders
        Then the response status code should be 200
        And the response contains only orders with active status for agent

    Scenario: Agent views active orders when they have no active orders
        Given an agent with ID 1 has only completed orders
        When the agent requests their active orders
        Then the response status code should be 200
        And the response contains an empty list

    Scenario: Agent views active orders when they have no orders at all
        Given an agent with ID 1 has no orders
        When the agent requests their active orders
        Then the response status code should be 200
        And the response contains an empty list

    Scenario: Agent active orders excludes delivered orders
        Given an agent with ID 1 has an order with status Delivered
        When the agent requests their active orders
        Then the response status code should be 200
        And the response contains an empty list

    Scenario: Agent active orders excludes rejected orders
        Given an agent with ID 1 has an order with status Rejected
        When the agent requests their active orders
        Then the response status code should be 200
        And the response contains an empty list
