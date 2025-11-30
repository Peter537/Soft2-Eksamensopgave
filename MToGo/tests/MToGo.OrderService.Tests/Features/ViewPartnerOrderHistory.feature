Feature: View Partner Order History
    As a partner
    I want to view past orders for my restaurant
    So that I can track sales and order patterns

    Scenario: Partner views order history with completed orders
        Given a partner with ID 1 has received orders
        When the partner requests their order history
        Then the response status code should be 200
        And the response contains a list of orders with dates, customers, items, and totals

    Scenario: Partner views order history with no orders
        Given a partner with ID 1 has no orders
        When the partner requests their order history
        Then the response status code should be 200
        And the partner response contains an empty list

    Scenario: Partner filters order history by date range
        Given a partner with ID 1 has orders from different dates
        When the partner requests order history with date range filter
        Then the response status code should be 200
        And only partner orders within the date range are returned

    Scenario: Partner filters order history with no matching orders
        Given a partner with ID 1 has orders outside the date range
        When the partner requests order history with date range filter
        Then the response status code should be 200
        And the partner response contains an empty list
