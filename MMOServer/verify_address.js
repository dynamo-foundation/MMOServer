var bitcoin = require('bitcoinjs-lib')
var bitcoinMessage = require('bitcoinjs-message')

var args = process.argv.slice(2)

var address = args[0];
var signature = args[1];
var message = args[2];

console.log(bitcoinMessage.verify(message, address, signature))
