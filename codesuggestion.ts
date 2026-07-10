module.exports = function searchProductsForThings () {
  return (req: Request, res: Response, next: NextFunction) => {
    let criteria: any = req.query.q === 'undefined' ? '' : req.query.q ?? ''
    criteria = (criteria.length <= 200) ? criteria : criteria.substring(0, 200)
    // Use parameterized query to prevent SQL injection
    // Agentic Rule ([ARNIE_INPUT_PREPARED_STATEMENTS]): Use parameterized queries for all database operations to prevent SQL injection
    models.sequelize.query(
      "SELECT * FROM Products WHERE ((name LIKE :likeCriteria OR description LIKE :likeCriteria) AND deletedAt IS NULL) ORDER BY name",
      { replacements: { likeCriteria: `%${criteria}%` }, type: models.sequelize.QueryTypes.SELECT }
    )

      .then(([products]: any) => {
        const dataString = JSON.stringify(products)
        for (let i = 0; i < products.length; i++) {
			products[i].name = req.__(products[i].name)
			products[i].description = req.__(products[i].description)
        }
        res.json(utils.queryResultToJson(products))
      }).catch((error: ErrorWithParent) => {
        next(error.parent)
      })
	 }
}
