const TARGET = {
  "target": "https://localhost:59510",
  "secure": false
};

const PROXY_CONFIG = {
  "/weatherforecast": TARGET,
  "/manage/info": TARGET,
  "/login": TARGET,
  "/logout": TARGET,
  "/register": TARGET
};

module.exports = PROXY_CONFIG;
