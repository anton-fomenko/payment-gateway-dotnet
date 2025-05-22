# Payment Gateway Challenge - .NET Version

## Table of Contents
- [Deployment](#deployment)
- [Assumptions](#assumptions)
- [Project Overview](#project-overview)
- [Design Decisions](#design-decisions)
  - [Exception Handling](#exception-handling)
  - [HTTP Status Codes](#http-status-codes)
  - [Retries and Idempotency](#retries-and-idempotency)
- [Testing](#testing)
- [Further Improvements](#further-improvements)

## Deployment
- **docker-compose:**
  - Run `docker-compose up` to start the services.
  - The API will be available at `http://localhost:5000`.
  - The bank simulator will be available at `http://localhost:8080`.
- **Example of POST request that returns Authorized status**:
    ```json
    {
      "cardNumber": "2222405343248877",
      "expiryMonth": 4,
      "expiryYear": 2025,
      "currency": "GBP",
      "amount": 100,
      "cvv": "123"
    }
## Assumptions

- **Authentication & Authorization:** Not implemented for this project. In a production environment, OAuth with client-credentials grant should be used to secure the API.
- **Single Merchant:** The system assumes a single merchant. In real-world scenarios, idempotency keys should be scoped per merchant to prevent data leakage between merchants.
- **Payment Rejected:** I assume that no need to create a payment resource in the database since the requirement says that "no payment could be created" in case of validation errors. So, I return a 400 Bad Request response with the error details.
- **"BankError" status:** I added this status to show to the merchant that payment failed because of the bank (or network error while communicating to the bank), and not because of our API. Since the bank simulator does not support idempotency keys, I didn't implement the retry logic, as it will lead to duplicate payments. In a real-world scenario, the bank should support idempotency keys to ensure consistency.

## Project Overview

- **PaymentGateway.Api**
  - **Controllers** - Handles HTTP requests and interacts with the service layer.
  - **Models** - Defines HTTP requests with data annotations for validation.
- **PaymentGateway.Services** - Contains business logic for processing payments.
- **PaymentGateway.Infrastructure** 
  - **Clients** - Communication with the bank.
  - **Repositories** - Database communication.
- **PaymentGateway.Domain** - Defines domain classes (Payment).

## Design Decisions

### Exception Handling

- **Middleware:** Default middleware is used to catch unhandled exceptions and convert them into `ProblemDetails` responses according to Problem Details Standard (RFC 7807).
- **Validation Errors:** Managed through data annotations, resulting in `400 Bad Request` without creating a payment resource. Title of the error is "Payment Rejected".
- **Business Flow Failures:** Scenarios like "Declined" and "BankError" are treated as normal responses and do not throw exceptions. Instead, they return appropriate statuses without disrupting the payment resource creation process.
- **Result Pattern in `BankClient`:** Utilized to handle success and failure without relying on exceptions, improving performance and clarity.

### HTTP Status Codes

- **201 Created:** Payment resource successfully created with statuses: Authorized, Declined, or BankError.
- **400 Bad Request:** Payment request rejected due to validation errors; no resource created.
- **500 Internal Server Error:** Unexpected errors with an empty response body in production environments.
- Other codes, such as 401 (Unauthenticated), 429 (Too Many Requests) are not used in this project, but should be considered for a production-ready system.

### Retries and Idempotency

- **Idempotency Keys:** Implemented to ensure that retrying a request with the same key does not create duplicate payments.
  - **MemoryCache:** Used for storing idempotency keys; suitable for single-instance deployments.
  - **Production Consideration:** A distributed cache like Redis is recommended for scalability.
- **Bank Simulator Limitations:** The simulator does not support idempotency keys, so I didn't implement the retry logic, as it could lead to duplicate payments. In case of BankError, the payment could still be created on the bank side, but we won't know about it. In a real-world scenario, the bank should support idempotency keys to ensure consistency.
- **Payment Resource Creation:** Even if the bank operation fails due to network errors, the payment resource is created on our side for reconciliation purposes. In real world project we will need to have a webhook or polling solution on the bank side to receive fresh updates about the status of the payment.

## Testing

- **End-to-end Tests:** Test the full flow from the API to the bank simulator. These tests are excluded from CI to keep the pipeline fast and deterministic. Run them locally with dotnet test --filter Category=EndToEnd after starting docker-compose up.
- **Integration Tests:** Covers validation, since I rely on framework features such as data annotations and default model binding, and these can't be tested in isolation.
- **Unit Tests:** Cover the classes in isolation.

## Further Improvements

1. **Security Enhancements:**
   - Implement OAuth for securing API endpoints.
   - Encrypt sensitive data both at rest and in transit.
   - Tokenize card details to comply with PCI DSS standards.

2. **Scalability:**
   - Transition to a microservices architecture to handle multiple payment methods.
   - Use Kubernetes for container orchestration to manage service scaling.

3. **API Enhancements:**
   - Develop a UI widget for merchants to tokenize card information client-side.
   - Introduce API versioning to manage backward-incompatible changes.
   - Add support for webhooks to notify merchants of payment status changes.
   - Add granular error reasons for payments with Declined statuses to help merchants to understand what happened. Such as "NotEnoughMoney", "CardExpired", etc.

4. **Testing and CI/CD:**
   - Integrate comprehensive testing into the CI/CD pipeline.

5. **Distributed Caching:**
   - Replace `MemoryCache` with Redis or another distributed caching solution to support multi-instance deployments.

6. **Logging and Monitoring:**
   - Implement structured logging and monitoring to track payment processing and system health.
   - Use tools like ELK Stack or Prometheus for observability.

7. **Availability:**
    - Use circuit breakers to prevent cascading failures.
    - Add Reverse Proxy to handle rate limiting and load balancing.
