protected override string CommandText =>
    """
    SELECT * FROM [Table] WHERE [ID] = @id
    """;

  using var cmd = new SqlCommand(CommandText, conn);
