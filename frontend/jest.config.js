module.exports = {
  testEnvironment: "jsdom",
  roots: ["<rootDir>/tests/unit"],
  moduleFileExtensions: ["ts", "tsx", "js", "jsx"],
  moduleNameMapper: {
    "^@/(.*)$": "<rootDir>/$1"
  },
  setupFilesAfterEnv: ["<rootDir>/node_modules/@testing-library/jest-dom/dist/index.js"],
  testMatch: ["**/?(*.)+(spec|test).[tj]s?(x)"],
  testPathIgnorePatterns: ["<rootDir>/tests/e2e/"],
  transform: {
    "^.+\\.(ts|tsx)$": [
      "ts-jest",
      {
        tsconfig: {
          jsx: "react-jsx"
        }
      }
    ]
  },
  transformIgnorePatterns: ["/node_modules/"]
};