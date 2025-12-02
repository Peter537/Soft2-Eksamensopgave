Feature: View Order History
    As a customer
    I want to view past orders
    So that I can track spending

    Scenario: Customer views order history when logged in
        Given a customer with ID 1 has placed orders
        When the customer requests their order history
        Then the response status code should be 200
        And the response contains a list of orders with dates, partners, items, and totals

    Scenario: Customer views order history with no orders
        Given a customer with ID 1 has no orders
        When the customer requests their order history
        Then the response status code should be 200
        And the response contains an empty list

    Scenario: Customer filters order history by date range
        Given a customer with ID 1 has orders from different dates
        When the customer requests order history with date range filter
        Then the response status code should be 200
        And only orders within the date range are returned

    Scenario: Customer filters order history with no matching orders
        Given a customer with ID 1 has orders outside the date range
        When the customer requests order history with date range filter
        Then the response status code should be 200
        And the response contains an empty list
