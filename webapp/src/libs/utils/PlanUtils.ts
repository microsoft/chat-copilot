// Copyright (c) Microsoft. All rights reserved.

// Use user intent, if available. If not, use user input.
export const getPlanGoal = (description: string) => {
    const userIntentPrefix = 'User intent: ';
    const userIntentIndex = description.indexOf(userIntentPrefix);
    return userIntentIndex !== -1
        ? description.substring(userIntentIndex + userIntentPrefix.length).trim()
        : description
              .split('\n')
              .find((line: string) => line.startsWith('INPUT:'))
              ?.replace('INPUT:', '')
              .trim() ?? description;
};
