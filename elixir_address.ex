defmodule Vlx_address do
  @alphabet ~c(123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz)

  def eth_to_vlx(address) do
    try do
      eth_to_vlx!(address)
    rescue
      e -> {:error, e}
    else
      res -> {:ok, res}
    end
  end

  def vlx_to_eth(address) do
    try do
      vlx_to_eth!(address)
    rescue
      e -> {:error, e}
    else
      res -> {:ok, res}
    end
  end

  @doc """
  Encodes the given ethereum address to vlx format.
  """
  def eth_to_vlx!(address) do
    stripped_address =
      case address do
        "0x" <> addr when byte_size(addr) == 40 -> String.downcase addr
        _ -> raise ArgumentError, message: "Invalid address prefix or length"
      end

    checksum =
      stripped_address
      |> sha256
      |> sha256
      |> String.slice(0, 8)

    parsed_address =
      stripped_address <> checksum
      |> Integer.parse(16)

    case parsed_address do
      {is_integer, ""} -> nil
      _ -> raise ArgumentError, message: "Invalid address format"
    end

    encoded_address =
      parsed_address
      |> elem(0)
      |> b58_encode()
      |> String.pad_leading(33, "1")

    "V" <> encoded_address
  end

  @doc """
  Decodes the given vlx address to ethereum format.
  """
  def vlx_to_eth!(address) do
    decoded_address =
      address
      |> String.trim_leading("V")
      |> b58_decode()
      |> Integer.to_string(16)
      |> String.downcase      
      |> String.pad_leading(48, "0")

    strings = Regex.run(~r/([0-9abcdef]+)([0-9abcdef]{8})$/, decoded_address)

    [_, short_address, extracted_checksum] =
      case strings do
        list when length(list) == 3 -> list
        _ -> raise ArgumentError, message: "Invalid address"
      end

    checksum = 
      short_address
      |> sha256
      |> sha256
      |> String.slice(0, 8)      

    if extracted_checksum != checksum do
      raise ArgumentError, message: "Invalid checksum"
    end

    ("0x" <> short_address) |> String.downcase()
  end

  @doc """
  Encodes the given integer.
  """
  defp b58_encode(x), do: _encode(x, [])

  @doc """
  Decodes the given string.
  """
  defp b58_decode(enc), do: _decode(enc |> to_char_list, 0)

  defp _encode(0, []), do: [@alphabet |> hd] |> to_string
  defp _encode(0, acc), do: acc |> to_string

  defp _encode(x, acc) do
    _encode(div(x, 58), [Enum.at(@alphabet, rem(x, 58)) | acc])
  end

  defp _decode([], acc), do: acc

  defp _decode([c | cs], acc) do
    _decode(cs, acc * 58 + Enum.find_index(@alphabet, &(&1 == c)))
  end

  @doc """
  Sha256 and convert to hex
  """
  defp sha256(x) do
    :crypto.hash(:sha256, x) 
    |> Base.encode16(case: :lower)
  end

  defp test_addr({eth, vlx}) do
    {:ok, vlx_conv} = eth_to_vlx(eth)
    {:ok, eth_conv} = vlx_to_eth(vlx)
    eth_match = eth == eth_conv
    vlx_match = vlx == vlx_conv
    
    IO.inspect binding()
    eth_match and vlx_match
  end

  def test_vlx() do
    eth_addresses = [
      "0x32Be343B94f860124dC4fEe278FDCBD38C102D88",
      "0x000000000000000000000000000000000000000f",
      "0xf000000000000000000000000000000000000000",
      "0x0000000000000000000000000000000000000001",
      "0x1000000000000000000000000000000000000000",
      "0x0000000000000000000000000000000000000000",
      "0xffffffffffffffffffffffffffffffffffffffff",
      "0xf00000000000000000000000000000000000000f"
    ]

    vlx_addresses = [
      "V5dJeCa7bmkqmZF53TqjRbnB4fG6hxuu4f",
      "V111111111111111111111111112jSS6vy",
      "VNt1B3HD3MghPihCxhwMxNKRerBPPbiwvZ",
      "V111111111111111111111111111CdXjnE",
      "V2Tbp525fpnBRiSt4iPxXkxMyf5ZX7bGAJ",
      "V1111111111111111111111111113iMDfC",
      "VQLbz7JHiBTspS962RLKV8GndWFwdcRndD",
      "VNt1B3HD3MghPihCxhwMxNKRerBR4azAjj"
    ]

    eth_addresses
    |> Enum.map(&String.downcase/1)
    |> Enum.zip(vlx_addresses)
    |> Enum.map(&test_addr/1)
    |> IO.inspect(label: "correct address checks")

    faulty_eth = [
      "32be343b94f860124dc4fee278fdcbd38c102d88",
      "0x32be343b94f860124dc4fee278fdcbd38c102",
      "kxf00000000000000000000000000000000000000f",
      "0xf0000000000000000000000000000000000kk000",
      "0xf00000000000000000000000000000000000000k",      
      "0xk00000000000000000000000000000000000000f"
    ]
        
    faulty_eth
    |> Enum.map(&eth_to_vlx/1)    
    |> Enum.all?(&match?({:error, _}, &1))
    |> IO.inspect(label: "faulty eth address checks")

    faulty_vlx = [
      "VOOJeCa7bmkqmZF53TqjRbnB4fG6hxuu4f",
      "VII1111111111111111111111112jSS6vy",
      "VNt1B3HD3MghPihCxhwMxNKRerBPPbixvZ",
      "V111111111113111111111111111CdXjnE",
      "V2Tbp525fpnBRiSt4XkxMyf5ZX7bGAJ",
      "VQLbz7JHiBTspS962RLKV8GndWFwdc"
    ]

    faulty_vlx
    |> Enum.map(&vlx_to_eth/1)
    |> Enum.all?(&match?({:error, _}, &1))
    |> IO.inspect(label: "faulty vlx address checks")

    nil
  end
end
