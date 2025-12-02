Feature: Reject Order
    As a partner
    I want to reject placed order
    So that customer is notified of order rejection

    Scenario: Successfully reject order with Placed status without reason
        Given an order exists with Placed status
        When the partner rejects the order without reason
        Then the order status should be Rejected
        And the response status code should be 204

    Scenario: Successfully reject order with Placed status with reason
        Given an order exists with Placed status
        When the partner rejects the order with reason "Out of ingredients"
        Then the order status should be Rejected
        And the response status code should be 204

    Scenario: OrderRejected kafka event published on rejection
        Given an order exists with Placed status
        When the partner rejects the order with reason "Kitchen closed"
        Then OrderRejected kafka event is published

    Scenario: Cannot reject order that is not in Placed status
        Given an order exists with Accepted status
        When the partner rejects the order without reason
        Then the response status code should be 400
