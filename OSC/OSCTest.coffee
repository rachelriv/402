# https://www.npmjs.com/package/osc-receiver

OSCReceiver = require 'osc-receiver'
receiver = new OSCReceiver

console.log 'starting OSC Receiver'
receiver.bind 8000

receiver.on '/test', (a) ->
  console.log a
