const ASPIRE_ENDPOINT = process.env['services__privateapi__https__0'] || process.env['services__privateapi__http__0'] || 'https://localhost:7002';

console.log('PrivateApp proxy configured for:', ASPIRE_ENDPOINT);

const config = {
  '/api/*': {
    target: ASPIRE_ENDPOINT,
    secure: true,
    changeOrigin: true,
    logLevel: 'info',
    onError: (err, req, res) => {
      console.error('Proxy error:', err);
    },
    onProxyReq: (proxyReq, req, res) => {
      console.log('Proxying request:', req.method, req.url, '→', ASPIRE_ENDPOINT + req.url);
    }
  },
  '/hub/*': {
    target: ASPIRE_ENDPOINT,
    secure: true,
    changeOrigin: true,
    ws: true, // Enable WebSocket proxying for SignalR
    logLevel: 'info'
  }
};

module.exports = config;