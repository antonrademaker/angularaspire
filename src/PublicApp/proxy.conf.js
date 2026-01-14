const ASPIRE_ENDPOINT = process.env['services__publicapi__https__0'] || process.env['services__publicapi__http__0'] || 'https://localhost:7069';

console.log('PublicApp proxy configured for:', ASPIRE_ENDPOINT);
console.log('Environment variables:', {
  https: process.env['services__publicapi__https__0'],
  http: process.env['services__publicapi__http__0']
});

const config = {
  '/api': {
    target: ASPIRE_ENDPOINT,
    secure: false,
    changeOrigin: true,
    logLevel: 'debug',
    onError: (err, req, res) => {
      console.error('Proxy error:', err);
    },
    onProxyReq: (proxyReq, req, res) => {
      console.log('Proxying request:', req.method, req.url, '→', ASPIRE_ENDPOINT + req.url);
    }
  },
  '/hub': {
    target: ASPIRE_ENDPOINT,
    secure: false,
    changeOrigin: true,
    ws: true, // Enable WebSocket proxying for SignalR
    logLevel: 'info'
  }
};

module.exports = config;
