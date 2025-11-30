Feature: View Order Details
    As a user (customer, partner, or agent)
    I want to view details of a specific order
    So that I can see complete order information

    Scenario: Customer views their own order details
        Given a customer with ID 1 has an order with ID 1
        When the customer requests order details for order ID 1
        Then the response status code should be 200
        And the response contains order details including items, status, dates, and totals

    Scenario: Partner views order details for their restaurant
        Given a partner with ID 1 has an order with ID 1
        When the partner requests order details for order ID 1
        Then the response status code should be 200
        And the response contains order details including items, status, dates, and totals

    Scenario: Agent views order details for their assigned delivery
        Given an agent with ID 1 is assigned to order with ID 1
        When the agent requests order details for order ID 1
        Then the response status code should be 200
        And the response contains order details including items, status, dates, and totals

    Scenario: Customer cannot view order belonging to another customer
        Given a customer with ID 2 has an order with ID 1
        And a customer with ID 1 is authenticated
        When the customer requests order details for order ID 1
        Then the response status code should be 403

    Scenario: Partner cannot view order from another restaurant
        Given a partner with ID 2 has an order with ID 1
        And a partner with ID 1 is authenticated
        When the partner requests order details for order ID 1
        Then the response status code should be 403

    Scenario: Agent cannot view order not assigned to them
        Given an agent with ID 2 is assigned to order with ID 1
        And an agent with ID 1 is authenticated
        When the agent requests order details for order ID 1
        Then the response status code should be 403

    Scenario: Requesting non-existent order returns 404
        Given a customer with ID 1 is authenticated
        When the customer requests order details for order ID 99999
        Then the response status code should be 404
