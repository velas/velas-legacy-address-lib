<?php
function ethToVlx(string $address): string
{
    $SIGNATURE = '123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz';
    $BASE58_LENGTH = '58';
    $BASE256_LENGTH = '256';

    $string = (string) $address;
    $string = strtolower(str_replace('0x', '', $string));

    if (empty($string)) {
        throw new InvalidArgumentException('Empty address');
    }

    if (strlen($string) !== 40) {
        throw new InvalidArgumentException('Invalid address length');
    }

    $checksum = substr(hash("sha256", hash("sha256", $string)), 0, 8);
    $long_address = $string . $checksum;
    $string = hex2bin($long_address);

    $bytes = array_values(array_map(function ($byte) {
        return (string) $byte;
    }, unpack('C*', $string)));
    $base10 = $bytes[0];
    // Convert string into base 10
    for ($i = 1, $l = count($bytes); $i < $l; $i++) {
        $base10 = bcmul($base10, $BASE256_LENGTH);
        $base10 = bcadd($base10, $bytes[$i]);
    }

    // Convert base 10 to base 58 string
    $base58 = '';
    while ($base10 >= $BASE58_LENGTH) {
        $div = bcdiv($base10, $BASE58_LENGTH, 0);
        $mod = bcmod($base10, $BASE58_LENGTH);
        $base58 .= $SIGNATURE[$mod];
        $base10 = $div;
    }
    if ($base10 > 0) {
        $base58 .= $SIGNATURE[$base10];
    }

    // Base 10 to Base 58 requires conversion
    $base58 = strrev($base58);
    // Add leading zeros
    $l = 33 - strlen($base58) + 1;
    for ($i = 1; $i < $l; $i++) {
        $base58 = $SIGNATURE[0] . $base58;
    }

    return 'V' . $base58;
}

function vlxToEth(string $address): string
{
    if (empty($address)) {
        throw new InvalidArgumentException('Empty address');
    }
    $SIGNATURE = '123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz';
    $BASE58_LENGTH = '58';
    $BASE256_LENGTH = '256';

    $indexes = array_flip(str_split($SIGNATURE));
    $chars = str_split($address);
    array_shift($chars);
    // Check for invalid characters in the supplied base58 string
    foreach ($chars as $char) {
        if (isset($indexes[$char]) === false) {
            throw new InvalidArgumentException('Argument $base58 contains invalid characters. ($char: "' . $char . '" | $base58: "' . $address . '") ');
        }
    }

    // Convert from base58 to base10
    $decimal = (string) $indexes[$chars[0]];

    for ($i = 1, $l = count($chars); $i < $l; $i++) {
        $decimal = bcmul($decimal, $BASE58_LENGTH);
        $decimal = bcadd($decimal, (string) $indexes[$chars[$i]]);
    }

    // Convert from base10 to base256 (8-bit byte array)
    $output = '';
    while ($decimal > 0) {
        $byte = bcmod($decimal, $BASE256_LENGTH);
        $output = pack('C', $byte) . $output;
        $decimal = bcdiv($decimal, $BASE256_LENGTH, 0);
    }

    $l = 24 - strlen($output) + 1;
    for ($i = 1; $i < $l; $i++) {
        $output = "\x0" . $output;
    }

    $long_address = bin2hex($output);
    $address_checksum = substr($long_address, 40, 8);
    $address = substr($long_address, 0, -8);
    $checksum = substr(hash("sha256", hash("sha256", $address)), 0, 8);

    if ($checksum !== $address_checksum) {
        throw new InvalidArgumentException('Invalid address, checksum');
    }
    if (strlen($address) !== 40) {
        throw new InvalidArgumentException('Invalid address length');
    }

    return '0x' . $address;
}

$eth_addresses = [
    "0x32Be343B94f860124dC4fEe278FDCBD38C102D88",
    "0x000000000000000000000000000000000000000f",
    "0xf000000000000000000000000000000000000000",
    "0x0000000000000000000000000000000000000001",
    "0x1000000000000000000000000000000000000000",
    "0x0000000000000000000000000000000000000000",
    "0xffffffffffffffffffffffffffffffffffffffff",
    "0xf00000000000000000000000000000000000000f"
];

$vlx_addresses = [
    "V5dJeCa7bmkqmZF53TqjRbnB4fG6hxuu4f",
    "V111111111111111111111111112jSS6vy",
    "VNt1B3HD3MghPihCxhwMxNKRerBPPbiwvZ",
    "V111111111111111111111111111CdXjnE",
    "V2Tbp525fpnBRiSt4iPxXkxMyf5ZX7bGAJ",
    "V1111111111111111111111111113iMDfC",
    "VQLbz7JHiBTspS962RLKV8GndWFwdcRndD",
    "VNt1B3HD3MghPihCxhwMxNKRerBR4azAjj"
];

foreach ($eth_addresses as $i => $eth_address) {
    $vlx_address = ethToVlx($eth_address);
    $restored_eth = vlxToEth($vlx_address);
    echo "Eth address: " . $eth_address . "\r\n";
    echo "Vlx address: " . $vlx_address . "\r\n";
    echo "is valid: " . ($vlx_address === $vlx_addresses[$i]) . "\r\n";
    echo "is valid: " . ($restored_eth === strtolower($eth_address)) . "\r\n";
}

foreach ($vlx_addresses as $i => $vlx_address) {
    $eth_address = vlxToEth($vlx_address);
    $restored_vlx = ethToVlx($eth_address);
    echo "Vlx address: " . $vlx_address . "\r\n";
    echo "Eth address: " . $eth_address . "\r\n";
    echo "is_valid: " . ($eth_address === strtolower($eth_addresses[$i])) . "\r\n";
    echo "is valid: " . ($restored_vlx === $vlx_address) . "\r\n";
}
