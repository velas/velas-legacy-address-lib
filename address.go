package main

import (
	"crypto/sha256"
	"encoding/hex"
	"fmt"
	"regexp"
	"strings"

	"github.com/btcsuite/btcutil/base58"
	"github.com/pkg/errors"
)

func main() {
	eth := "0x32Be343B94f860124dC4fEe278FDCBD38C102D88"
	vel := "V5dJeCa7bmkqmZF53TqjRbnB4fG6hxuu4f"

	encoded, err := EthToVlx(eth)
	if err != nil {
		panic(err)
	}

	decoded, err := VlxToEth(vel)
	if err != nil {
		panic(err)
	}

	if encoded == vel && decoded == strings.ToLower(eth) {
		fmt.Println("success")
	}

	ethAddresses := []string{
		"0x32Be343B94f860124dC4fEe278FDCBD38C102D88",
		"0x000000000000000000000000000000000000000f",
		"0xf000000000000000000000000000000000000000",
		"0x0000000000000000000000000000000000000001",
		"0x1000000000000000000000000000000000000000",
		"0x0000000000000000000000000000000000000000",
		"0xffffffffffffffffffffffffffffffffffffffff",
	}

	vlxAddresses := []string{
		"V5dJeCa7bmkqmZF53TqjRbnB4fG6hxuu4f",
		"V111111111111111111111111112jSS6vy",
		"VNt1B3HD3MghPihCxhwMxNKRerBPPbiwvZ",
		"V111111111111111111111111111CdXjnE",
		"V2Tbp525fpnBRiSt4iPxXkxMyf5ZX7bGAJ",
		"V1111111111111111111111111113iMDfC",
		"VQLbz7JHiBTspS962RLKV8GndWFwdcRndD",
	}

	for i, eth := range ethAddresses {
		v, err := EthToVlx(eth)
		if err != nil {
			panic(err)
		}
		fmt.Println(i, ": ", " got: ", v, " expected: ", vlxAddresses[i], "result: ", v == vlxAddresses[i])
		if v != vlxAddresses[i] {
			panic("vel address don't match")
		}
	}

	for i, vlx := range vlxAddresses {
		e, err := VlxToEth(vlx)
		if err != nil {
			panic(err)
		}
		fmt.Println(i, ": ", " got: ", e, " expected: ", ethAddresses[i], "result: ", e == strings.ToLower(ethAddresses[i]))
		if vlx != vlxAddresses[i] {
			panic("eth address don't match")
		}
	}
}

func EthToVlx(address string) (string, error) {
	if len(address) != 42 {
		return "", errors.New("invalid eth address length")
	}

	if !strings.HasPrefix(address, "0x") {
		return "", errors.New("invalid eth address")
	}

	strippedAddressHex := strings.ToLower(strings.TrimPrefix(address, "0x"))

	checksum := sha(sha(strippedAddressHex))[:8]
	raw := strippedAddressHex + checksum

	dec, err := hex.DecodeString(raw)
	if err != nil {
		return "", errors.Wrap(err, "failed to decode long address")
	}
	encoded := base58.Encode(dec)
	if len(encoded) < 33 {
		encoded = fmt.Sprintf("%s%s", strings.Repeat("1", 33-len(encoded)), encoded)
	}

	result := "V" + encoded
	return result, nil
}

func VlxToEth(address string) (string, error) {
	clean := strings.TrimPrefix(address, "V")
	decodedAddress := hex.EncodeToString(base58.Decode(clean))

	regex := regexp.MustCompile(`([0-9abcdef]+)([0-9abcdef]{8})$`)
	if !regex.MatchString(decodedAddress) {
		return "", errors.New("invalid decoded address")
	}

	matches := regex.FindStringSubmatch(decodedAddress)
	if len(matches) != 3 {
		return "", errors.New("invalid address")
	}

	for len(matches[1]) > 40 {
		if strings.HasPrefix(matches[1], "0") {
			matches[1] = strings.TrimPrefix(matches[1], "0")
		} else {
			return "", errors.New("invalid match")
		}
	}

	checksum := sha(sha(matches[1]))[:8]

	if matches[2] != checksum {
		return "", errors.New("invalid checksum")
	}

	if len(matches[1]) != 40 {
		return "", errors.New("failed to get eth address")
	}

	return "0x" + matches[1], nil
}

func sha(raw string) string {
	hasher := sha256.New()
	hasher.Write([]byte(raw))
	return hex.EncodeToString(hasher.Sum(nil))
}
