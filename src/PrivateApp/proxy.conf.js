const ASPIRE_ENDPOINT = process.env['services__privateapi__https__0'] || process.env['services__privateapi__http__0'] || 'http://localhost:59765';

console.log('PrivateApp proxy configured for:', ASPIRE_ENDPOINT);
console.log('Environment variables:', {
  https: process.env['services__privateapi__https__0'],
  http: process.env['services__privateapi__http__0']
});

const config = {
  '/api': {
    target: ASPIRE_ENDPOINT,
    secure: false, // Allow self-signed certificates in development
    changeOrigin: true,
    logLevel: 'debug',
    onError: (err, req, res) => {
      console.error('Proxy error:', err);
    },
    onProxyReq: (proxyReq, req, res) => {
      console.log('Proxying request:', req.method, req.url, '→', ASPIRE_ENDPOINT + req.url);
    }
  },
  '/health': {
    target: ASPIRE_ENDPOINT,
    secure: false, // Allow self-signed certificates in development
    changeOrigin: true,
    logLevel: 'info'
  },
  '/alive': {
    target: ASPIRE_ENDPOINT,
    secure: false, // Allow self-signed certificates in development
    changeOrigin: true,
    logLevel: 'info'
  },
  '/hub/*': {
    target: ASPIRE_ENDPOINT,
    secure: false, // Allow self-signed certificates in development
    changeOrigin: true,
    ws: true, // Enable WebSocket proxying for SignalR
    logLevel: 'info'
  }
};

module.exports = config;
