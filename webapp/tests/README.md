# Copilot Chat Web App Scenario Tests

## How to set up the tests to run locally

### Install Playwright

Playwright is a dependency included in package.json. You just need to run `yarn install` followed by `yarn playwright install --with-deps` at the webapp/ root to install Playwright.

> (Optional) Install the [VS Code Extension](https://marketplace.visualstudio.com/items?itemName=ms-playwright.playwright).

### Set up App registrations

Follow the [instructions](https://github.com/microsoft/chat-copilot#optional-enable-backend-authentication-via-azure-ad) to create two app registrations. This is needed for the multi-user chat test.

### Configure the environment

-   Follow the [instructions](https://github.com/microsoft/chat-copilot#optional-enable-backend-authentication-via-azure-ad) to configure the `/webapi/appsettings.json` file.

-   You need two test accounts to run the multi-user chat. Make sure the two accounts are under the correct tenant. Enter the account credentials in the .env file.

### Running the tests

-   Open a terminal window to start the webapi.

-   Once the webapi is ready, run `yarn playwright test` in another terminal or use the [VS Code Extension](https://marketplace.visualstudio.com/items?itemName=ms-playwright.playwright).
