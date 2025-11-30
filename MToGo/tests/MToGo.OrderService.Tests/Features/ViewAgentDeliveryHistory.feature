Feature: View Agent Delivery History
    As an agent
    I want to view my past deliveries
    So that I can track my performance and earnings

    Scenario: Agent views delivery history with completed deliveries
        Given an agent with ID 1 has completed deliveries
        When the agent requests their delivery history
        Then the response status code should be 200
        And the response contains a list of deliveries with dates, partners, customers, and delivery fees

    Scenario: Agent views delivery history with no deliveries
        Given an agent with ID 1 has no deliveries
        When the agent requests their delivery history
        Then the response status code should be 200
        And the delivery response contains an empty list

    Scenario: Agent filters delivery history by date range
        Given an agent with ID 1 has deliveries from different dates
        When the agent requests delivery history with date range filter
        Then the response status code should be 200
        And only deliveries within the date range are returned

    Scenario: Agent filters delivery history with no matching deliveries
        Given an agent with ID 1 has deliveries outside the date range
        When the agent requests delivery history with date range filter
        Then the response status code should be 200
        And the delivery response contains an empty list
